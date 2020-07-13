using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public class CounterpartyAccountService : ICounterpartyAccountService
    {
        public CounterpartyAccountService(EdoContext context,
            IEntityLocker locker,
            IAccountBalanceAuditService auditService)
        {
            _context = context;
            _locker = locker;
            _auditService = auditService;
        }


        public async Task<Result<CounterpartyBalanceInfo>> GetBalance(int counterpartyId, Currencies currency)
        {
            var accountInfo = await _context.CounterpartyAccounts
                .FirstOrDefaultAsync(a => a.Currency == currency && a.CounterpartyId == counterpartyId);

            return accountInfo == null
                ? Result.Failure<CounterpartyBalanceInfo>($"Payments with accounts for currency {currency} is not available for current counterparty")
                : Result.Ok(new CounterpartyBalanceInfo(accountInfo.Balance, accountInfo.Currency));
        }


        public async Task<Result> AddMoney(int counterpartyAccountId, PaymentData paymentData, UserInfo user)
        {
            var result = await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number");

            await using var accountLock = await GetEntityLock(result, r => r.Value);
            result = InsureLocked(result, accountLock);

            return await result
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(AddMoneyToCounterparty)
                    .Map(WriteAuditLog));

            bool IsReasonProvided(CounterpartyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAmountPositive(CounterpartyAccount account) => paymentData.Amount.IsGreaterThan(decimal.Zero);


            async Task<CounterpartyAccount> AddMoneyToCounterparty(CounterpartyAccount account)
            {
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task WriteAuditLog(CounterpartyAccount account)
            {
                var eventData = new CounterpartyAccountBalanceLogEventData(paymentData.Reason, account.Balance);
                await _auditService.Write(AccountEventType.CounterpartyAdd,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    null);
            }
        }


        public async Task<Result> SubtractMoney(int counterpartyAccountId, PaymentCancellationData data, UserInfo user)
        {
            var result = await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, data), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number");

            await using var accountLock = await GetEntityLock(result, r => r.Value);
            InsureLocked(result, accountLock);

            return await result
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(SubtractMoney)
                    .Map(WriteAuditLog));

            bool IsAmountPositive(CounterpartyAccount account) => data.Amount.IsGreaterThan(decimal.Zero);


            async Task<CounterpartyAccount> SubtractMoney(CounterpartyAccount account)
            {
                account.Balance -= data.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task WriteAuditLog(CounterpartyAccount account)
            {
                var eventData = new CounterpartyAccountBalanceLogEventData(null, account.Balance);
                await _auditService.Write(AccountEventType.CounterpartySubtract,
                    account.Id,
                    data.Amount,
                    user,
                    eventData,
                    null);
            }
        }


        public async Task<Result> TransferToDefaultAgency(int counterpartyAccountId, MoneyAmount amount, UserInfo user)
        {
            var result = await GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, amount), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number");

            await using var counterpartyAccountLock = await GetEntityLock(result, r => r.Value);
            result = InsureLocked(result, counterpartyAccountLock);

            var result2 = await result
                .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                .Bind(GetDefaultAgencyAccount);

            await using var agencyAccountLock = await GetEntityLock(result2, r => r.Value.paymentAccount);
            result2 = InsureLocked(result2, agencyAccountLock);

            return await result2
                .BindWithTransaction(_context, accounts => Result.Success(accounts)
                    .Map(TransferMoney)
                    .Map(WriteAuditLog));

            bool IsAmountPositive(CounterpartyAccount account) => amount.Amount.IsGreaterThan(decimal.Zero);

            bool IsBalanceSufficient(CounterpartyAccount account) => account.Balance.IsGreaterOrEqualThan(amount.Amount);


            async Task<Result<(CounterpartyAccount counterpartyAccount, PaymentAccount paymentAccount)>> GetDefaultAgencyAccount(
                CounterpartyAccount counterpartyAccount)
            {
                var defaultAgency = await _context.Agencies
                    .Where(a => a.CounterpartyId == counterpartyAccount.CounterpartyId && a.ParentId == null)
                    .SingleOrDefaultAsync();

                if (defaultAgency == null)
                    return Result.Failure<(CounterpartyAccount, PaymentAccount)>("Could not find the default agency of the account owner");

                var paymentAccount = await _context.PaymentAccounts
                    .Where(a => a.AgencyId == defaultAgency.Id && a.Currency == amount.Currency)
                    .SingleOrDefaultAsync();

                if (paymentAccount == null)
                    return Result.Failure<(CounterpartyAccount, PaymentAccount)>("Could not find the default agency payment account");

                return Result.Ok<(CounterpartyAccount, PaymentAccount)>((counterpartyAccount, paymentAccount));
            }


            async Task<(CounterpartyAccount, PaymentAccount)> TransferMoney((CounterpartyAccount, PaymentAccount) accounts)
            {
                var (counterpartyAccount, paymentAccount) = accounts;

                counterpartyAccount.Balance -= amount.Amount;
                _context.Update(counterpartyAccount);

                paymentAccount.Balance += amount.Amount;
                _context.Update(paymentAccount);

                await _context.SaveChangesAsync();

                return (counterpartyAccount, paymentAccount);
            }


            async Task WriteAuditLog((CounterpartyAccount, PaymentAccount) accounts)
            {
                var (counterpartyAccount, paymentAccount) = accounts;

                var counterpartyEventData = new CounterpartyAccountBalanceLogEventData(null, counterpartyAccount.Balance);
                await _auditService.Write(AccountEventType.CounterpartyTransferToAgency,
                    counterpartyAccount.Id,
                    amount.Amount,
                    user,
                    counterpartyEventData,
                    null);

                var agencyEventData = new AccountBalanceLogEventData(null, paymentAccount.Balance,
                    paymentAccount.CreditLimit, paymentAccount.AuthorizedBalance);
                await _auditService.Write(AccountEventType.CounterpartyTransferToAgency,
                    paymentAccount.Id,
                    amount.Amount,
                    user,
                    agencyEventData,
                    null);
            }
        }


        private async Task<Result<CounterpartyAccount>> GetCounterpartyAccount(int counterpartyAccountId)
        {
            var account = await _context.CounterpartyAccounts.SingleOrDefaultAsync(p => p.Id == counterpartyAccountId);
            return account == default
                ? Result.Failure<CounterpartyAccount>("Could not find account")
                : Result.Ok(account);
        }

        private bool AreCurrenciesMatch(CounterpartyAccount account, PaymentData paymentData) => account.Currency == paymentData.Currency;

        private bool AreCurrenciesMatch(CounterpartyAccount account, MoneyAmount amount) => account.Currency == amount.Currency;

        private bool AreCurrenciesMatch(CounterpartyAccount account, PaymentCancellationData data) => account.Currency == data.Currency;


        private async Task<EntityLock<TEntity>> GetEntityLock<TResult, TEntity>(Result<TResult> result, Func<Result<TResult>, TEntity> entityGetter)
            where TEntity : IEntity =>
            result.IsSuccess
                ? await _locker.CreateLock<TEntity>(entityGetter(result).Id.ToString(), nameof(ICounterpartyAccountService))
                : default;


        private Result<TResult> InsureLocked<TResult, TEntity>(Result<TResult> result, EntityLock<TEntity> entityLock) =>
            result.IsSuccess 
                ? result.Ensure(_ => entityLock.Acquired, entityLock.Error)
                : result;


        private readonly IAccountBalanceAuditService _auditService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}