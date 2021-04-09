using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class CounterpartyAccountService : ICounterpartyAccountService
    {
        public CounterpartyAccountService(EdoContext context,
            IEntityLocker locker,
            IAccountBalanceAuditService auditService,
            ICounterpartyBillingNotificationService counterpartyBillingNotificationService)
        {
            _context = context;
            _locker = locker;
            _auditService = auditService;
            _counterpartyBillingNotificationService = counterpartyBillingNotificationService;
        }


        public async Task<Result<CounterpartyBalanceInfo>> GetBalance(int counterpartyId, Currencies currency)
        {
            var accountInfo = await _context.CounterpartyAccounts
                .FirstOrDefaultAsync(a => a.IsActive && a.Currency == currency && a.CounterpartyId == counterpartyId);

            return accountInfo == null
                ? Result.Failure<CounterpartyBalanceInfo>($"Payments with accounts for currency {currency} is not available for current counterparty")
                : Result.Success(new CounterpartyBalanceInfo(accountInfo.Balance, accountInfo.Currency));
        }


        public async Task<Result> AddMoney(int counterpartyAccountId, PaymentData paymentData, ApiCaller apiCaller)
        {
            return await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(AddMoneyToCounterparty)
                        .Map(WriteAuditLog)))
                .Tap(SendMailNotification);

            bool IsReasonProvided(CounterpartyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAmountPositive(CounterpartyAccount account) => paymentData.Amount.IsGreaterThan(decimal.Zero);


            async Task<CounterpartyAccount> AddMoneyToCounterparty(CounterpartyAccount account)
            {
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<CounterpartyAccount> WriteAuditLog(CounterpartyAccount account)
            {
                var eventData = new CounterpartyAccountBalanceLogEventData(paymentData.Reason, account.Balance);
                await _auditService.Write(AccountEventType.CounterpartyAdd,
                    account.Id,
                    paymentData.Amount,
                    apiCaller,
                    eventData,
                    null);

                return account;
            }


            Task SendMailNotification(CounterpartyAccount account)
                => _counterpartyBillingNotificationService.NotifyAdded(account.CounterpartyId, paymentData);
        }


        public async Task<Result> SubtractMoney(int counterpartyAccountId, PaymentCancellationData data, ApiCaller apiCaller)
        {
            return await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, data), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .BindWithLock(_locker, a => Result.Success(a)
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(SubtractMoney)
                        .Map(WriteAuditLog)));

            bool IsAmountPositive(CounterpartyAccount account) => data.Amount.IsGreaterThan(decimal.Zero);


            async Task<CounterpartyAccount> SubtractMoney(CounterpartyAccount account)
            {
                account.Balance -= data.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<CounterpartyAccount> WriteAuditLog(CounterpartyAccount account)
            {
                var eventData = new CounterpartyAccountBalanceLogEventData(null, account.Balance);
                await _auditService.Write(AccountEventType.CounterpartySubtract,
                    account.Id,
                    data.Amount,
                    apiCaller,
                    eventData,
                    null);

                return account;
            }
        }


        public async Task<Result> TransferToDefaultAgency(int counterpartyAccountId, MoneyAmount amount, ApiCaller apiCaller)
        {
            return await Result.Success(counterpartyAccountId)
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .Bind(GetCounterpartyAccount)
                .Ensure(a => AreCurrenciesMatch(a, amount), "Account and payment currency mismatch")
                .Bind(GetDefaultAgencyAccount)
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                    .BindWithTransaction(_context, accounts => Result.Success(accounts)
                        .Map(TransferMoney)
                        .Map(WriteAuditLog)));

            bool IsAmountPositive(int _) => amount.Amount.IsGreaterThan(decimal.Zero);


            bool IsBalanceSufficient((CounterpartyAccount counterpartyAccount, AgencyAccount agencyAccount) accounts)
                => accounts.counterpartyAccount.Balance.IsGreaterOrEqualThan(amount.Amount);


            async Task<Result<(CounterpartyAccount, AgencyAccount)>> GetDefaultAgencyAccount(CounterpartyAccount counterpartyAccount)
            {
                var defaultAgency = await _context.Agencies
                    .Where(a => a.CounterpartyId == counterpartyAccount.CounterpartyId && a.ParentId == null)
                    .SingleOrDefaultAsync();

                if (defaultAgency == null)
                    return Result.Failure<(CounterpartyAccount, AgencyAccount)>("Could not find the default agency of the account owner");

                var agencyAccount = await _context.AgencyAccounts
                    .Where(a => a.AgencyId == defaultAgency.Id && a.Currency == amount.Currency)
                    .SingleOrDefaultAsync();

                if (agencyAccount == null)
                    return Result.Failure<(CounterpartyAccount, AgencyAccount)>("Could not find the default agency account");

                return Result.Success<(CounterpartyAccount, AgencyAccount)>((counterpartyAccount, agencyAccount));
            }


            async Task<(CounterpartyAccount, AgencyAccount)> TransferMoney((CounterpartyAccount, AgencyAccount) accounts)
            {
                var (counterpartyAccount, agencyAccount) = accounts;

                counterpartyAccount.Balance -= amount.Amount;
                _context.Update(counterpartyAccount);

                agencyAccount.Balance += amount.Amount;
                _context.Update(agencyAccount);

                await _context.SaveChangesAsync();

                return (counterpartyAccount, agencyAccount);
            }


            async Task<(CounterpartyAccount, AgencyAccount)> WriteAuditLog((CounterpartyAccount, AgencyAccount) accounts)
            {
                var (counterpartyAccount, agencyAccount) = accounts;

                var counterpartyEventData = new CounterpartyAccountBalanceLogEventData(null, counterpartyAccount.Balance);
                await _auditService.Write(AccountEventType.CounterpartyTransferToAgency,
                    counterpartyAccount.Id,
                    amount.Amount,
                    apiCaller,
                    counterpartyEventData,
                    null);

                var agencyEventData = new AccountBalanceLogEventData(null, agencyAccount.Balance);
                await _auditService.Write(AccountEventType.CounterpartyTransferToAgency,
                    agencyAccount.Id,
                    amount.Amount,
                    apiCaller,
                    agencyEventData,
                    null);

                return (counterpartyAccount, agencyAccount);
            }
        }


        public async Task<Result> DecreaseManually(int counterpartyAccountId, PaymentData data, ApiCaller apiCaller)
        {
            return await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, data), "Account and payment currency mismatch")
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .BindWithLock(_locker, a => Result.Success(a)
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(Decrease)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(CounterpartyAccount account) => !string.IsNullOrEmpty(data.Reason);

            bool IsAmountPositive(CounterpartyAccount account) => data.Amount.IsGreaterThan(decimal.Zero);


            async Task<CounterpartyAccount> Decrease(CounterpartyAccount account)
            {
                account.Balance -= data.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<CounterpartyAccount> WriteAuditLog(CounterpartyAccount account)
            {
                var eventData = new CounterpartyAccountBalanceLogEventData(data.Reason, account.Balance);
                await _auditService.Write(AccountEventType.ManualDecrease,
                    account.Id,
                    data.Amount,
                    apiCaller,
                    eventData,
                    null);

                return account;
            }
        }


        public Task<List<CounterpartyAccountInfo>> GetAccounts(int counterpartyId)
        {
            return _context.CounterpartyAccounts
                .Where(c => c.CounterpartyId == counterpartyId)
                .Select(c => new CounterpartyAccountInfo
                {
                    Id = c.Id,
                    Currency = c.Currency,
                    Balance = new MoneyAmount
                    {
                        Amount = c.Balance,
                        Currency = c.Currency
                    }
                })
                .ToListAsync();
        }


        public async Task<Result> IncreaseManually(int counterpartyAccountId, PaymentData data, ApiCaller apiCaller)
        {
            return await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, data), "Account and payment currency mismatch")
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .BindWithLock(_locker, a => Result.Success(a)
                    .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                    .BindWithTransaction(_context, account => Result.Success(account)
                        .Map(Increase)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(CounterpartyAccount account) => !string.IsNullOrEmpty(data.Reason);

            bool IsAmountPositive(CounterpartyAccount account) => data.Amount.IsGreaterThan(decimal.Zero);


            async Task<CounterpartyAccount> Increase(CounterpartyAccount account)
            {
                account.Balance += data.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<CounterpartyAccount> WriteAuditLog(CounterpartyAccount account)
            {
                var eventData = new CounterpartyAccountBalanceLogEventData(data.Reason, account.Balance);
                await _auditService.Write(AccountEventType.ManualIncrease,
                    account.Id,
                    data.Amount,
                    apiCaller,
                    eventData,
                    null);

                return account;
            }
        }


        private async Task<Result<CounterpartyAccount>> GetCounterpartyAccount(int counterpartyAccountId)
        {
            var account = await _context.CounterpartyAccounts.SingleOrDefaultAsync(p => p.IsActive && p.Id == counterpartyAccountId);
            return account == default
                ? Result.Failure<CounterpartyAccount>("Could not find account")
                : Result.Success(account);
        }


        private bool AreCurrenciesMatch(CounterpartyAccount account, PaymentData paymentData) => account.Currency == paymentData.Currency;

        private bool AreCurrenciesMatch(CounterpartyAccount account, MoneyAmount amount) => account.Currency == amount.Currency;

        private bool AreCurrenciesMatch(CounterpartyAccount account, PaymentCancellationData data) => account.Currency == data.Currency;


        private readonly IAccountBalanceAuditService _auditService;
        private readonly ICounterpartyBillingNotificationService _counterpartyBillingNotificationService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}