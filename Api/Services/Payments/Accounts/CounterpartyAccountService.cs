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


        public Task<Result> AddMoney(int counterpartyAccountId, PaymentData paymentData, UserInfo user)
        {
            return GetCounterpartyAccount(counterpartyAccountId)
                .Bind(ValidatePayment)
                .Bind(UpdateBalance);


            Result<CounterpartyAccount> ValidatePayment(CounterpartyAccount account)
            {
                return Result.Success(account)
                    .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                    .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                    .Ensure(IsAmountPositive, "Payment amount must be a positive number");
            }

            bool IsReasonProvided(CounterpartyAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAmountPositive(CounterpartyAccount account) => paymentData.Amount.IsGreaterThan(decimal.Zero);


            Task<Result> UpdateBalance(CounterpartyAccount account)
            {
                return LockCounterpartyAccount(account)
                    .BindWithTransaction(_context, account => Result.Ok(account)
                        .Map(AddMoneyToCounterparty)
                        .Map(WriteAuditLog))
                    .Finally(result => UnlockCounterpartyAccount(result, counterpartyAccountId));
            }

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
                    user,
                    eventData,
                    null);

                return account;
            }
        }


        public Task<Result> SubtractMoney(int counterpartyAccountId, PaymentCancellationData data, UserInfo user)
        {
            return GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, data), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .Bind(LockCounterpartyAccount)
                .BindWithTransaction(_context, account => Result.Ok(account)
                    .Map(SubtractMoney)
                    .Map(WriteAuditLog))
                .Finally(result => UnlockCounterpartyAccount(result, counterpartyAccountId));

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
                    user,
                    eventData,
                    null);

                return account;
            }
        }


        public Task<Result> TransferToDefaultAgency(int counterpartyAccountId, MoneyAmount amount, UserInfo user)
        {
            return GetAccounts(counterpartyAccountId)
                .Bind(Validate)
                .Bind(Transfer);

            
            Task<Result<(CounterpartyAccount, PaymentAccount)>> GetAccounts(int counterpartyAccountId)
            {
                return GetCounterpartyAccount(counterpartyAccountId)
                    .Bind(GetDefaultAgencyAccount);
            }
            
            
            Result<(CounterpartyAccount, PaymentAccount)> Validate((CounterpartyAccount CounterpartyAccount, PaymentAccount AgencyAccount) accounts)
            {
                return Result.Success(accounts)
                    .Ensure(a => AreCurrenciesMatch(a.CounterpartyAccount, amount), "Account and payment currency mismatch")
                    .Ensure(a => IsAmountPositive(a.CounterpartyAccount), "Payment amount must be a positive number");
            }


            Task<Result> Transfer((CounterpartyAccount CounterpartyAccount, PaymentAccount AgencyAccount) accounts)
            {
                return LockAccounts(accounts)
                    .Ensure(a => IsBalanceSufficient(a.CounterpartyAccount), "Could not charge money, insufficient balance")
                    .BindWithTransaction(_context, accounts => Result.Ok(accounts)
                        .Map(TransferMoney)
                        .Map(WriteAuditLog))
                    .Finally(UnlockAccounts);
            }


            async Task<Result<(CounterpartyAccount CounterpartyAccount, PaymentAccount AgencyAccount)>> LockAccounts((CounterpartyAccount, PaymentAccount) accounts)
            {
                var (counterpartyAccount, agencyAccount) = accounts;
                var (_, isCounterpartyFailure, _, counterpartyError) = await LockCounterpartyAccount(counterpartyAccount);
                if (isCounterpartyFailure)
                    return Result.Failure<(CounterpartyAccount, PaymentAccount)>(counterpartyError);
                
                var (_, isAgencyFailure, _, agencyError) = await LockPaymentAccount(agencyAccount);
                if(isAgencyFailure)
                    return Result.Failure<(CounterpartyAccount, PaymentAccount)>(agencyError);

                return Result.Success(accounts);
            }


            bool IsAmountPositive(CounterpartyAccount account) => amount.Amount.IsGreaterThan(decimal.Zero);

            bool IsBalanceSufficient(CounterpartyAccount account) => account.Balance.IsGreaterOrEqualThan(amount.Amount);


            async Task<Result<(CounterpartyAccount, PaymentAccount)>> GetDefaultAgencyAccount(CounterpartyAccount counterpartyAccount)
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


            async Task<(CounterpartyAccount, PaymentAccount)> WriteAuditLog((CounterpartyAccount, PaymentAccount) accounts)
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

                return (counterpartyAccount, paymentAccount);
            }


            async Task<Result> UnlockAccounts(Result<(CounterpartyAccount, PaymentAccount)> result)
            {
                var (isSuccess, _, (counterpartyAccount, agencyAccount), error) = result;

                var newResult = isSuccess ? Result.Ok() : Result.Failure(error);

                if (counterpartyAccount != default)
                    await UnlockCounterpartyAccount(Result.Ok(counterpartyAccount), counterpartyAccount.Id);

                if (agencyAccount != default)
                    await UnlockAgencyAccount(Result.Ok(agencyAccount), agencyAccount.Id);

                return newResult;
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


        private async Task<Result<CounterpartyAccount>> LockCounterpartyAccount(CounterpartyAccount account)
        {
            var (isSuccess, _, error) = await _locker.Acquire<CounterpartyAccount>(account.Id.ToString(), nameof(IAccountPaymentProcessingService));
            return isSuccess
                ? Result.Ok(account)
                : Result.Failure<CounterpartyAccount>(error);
        }


        private async Task<Result> UnlockCounterpartyAccount(Result<CounterpartyAccount> result, int accountId)
        {
            await _locker.Release<CounterpartyAccount>(accountId.ToString());
            return result;
        }


        private async Task<Result<PaymentAccount>> LockPaymentAccount(PaymentAccount account)
        {
            var (isSuccess, _, error) = await _locker.Acquire<PaymentAccount>(account.Id.ToString(), nameof(IAccountPaymentProcessingService));
            return isSuccess
                ? Result.Ok(account)
                : Result.Failure<PaymentAccount>(error);
        }


        private async Task<Result> UnlockAgencyAccount(Result<PaymentAccount> result, int accountId)
        {
            await _locker.Release<PaymentAccount>(accountId.ToString());
            return result;
        }


        private readonly IAccountBalanceAuditService _auditService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}