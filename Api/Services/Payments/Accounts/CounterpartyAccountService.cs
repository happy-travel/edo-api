using System.Collections.Generic;
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
            var locks = new List<IEntityLock>();

            return GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .Bind(LockCounterpartyAccount)
                .BindWithTransaction(_context, account => Result.Ok(account)
                    .Map(AddMoneyToCounterparty)
                    .Map(WriteAuditLog))
                .Finally(ReleaseAccounts);


            Task<Result<TResult>> LockCounterpartyAccount<TResult>(TResult account) where TResult : IEntity => LockAccount(locks, account);

            Task<Result> ReleaseAccounts<TResult>(Result<TResult> result) => this.ReleaseAccounts(locks, result);

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
            var locks = new List<IEntityLock>();

            return GetCounterpartyAccount(counterpartyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, amount), "Account and payment currency mismatch")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .Bind(LockCounterpartyAccount)
                .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                .Bind(GetDefaultAgencyAccount)
                .Bind(LockAgencyAccount)
                .BindWithTransaction(_context, accounts => Result.Success(accounts)
                    .Map(TransferMoney)
                    .Tap(WriteAuditLog))
                .Finally(ReleaseAccounts);


            Task<Result<TResult>> LockCounterpartyAccount<TResult>(TResult account) where TResult : IEntity => LockAccount(locks, account);

            Task<Result> ReleaseAccounts<TResult>(Result<TResult> result) => this.ReleaseAccounts(locks, result);


            async Task<Result<(CounterpartyAccount, PaymentAccount)>> LockAgencyAccount(
                (CounterpartyAccount counterpartyAccount, PaymentAccount paymentAccount) accounts)
            {
                var (isSuccess, _, error) = await _locker.AddEntityLock(locks, accounts.paymentAccount, LockerName);
                return Result.Success(accounts).Ensure(_ => isSuccess, error);
            }

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


        private async Task<Result> UnlockPaymentAccount(Result<PaymentAccount> result, int accountId)
        {
            await _locker.Release<PaymentAccount>(accountId.ToString());
            return result;
        }

        async Task<Result<TResult>> LockAccount<TResult>(IList<IEntityLock> locksHolder, TResult account) where TResult : IEntity
        {
            var (isSuccess, _, error) = await _locker.AddEntityLock(locksHolder, account, LockerName);
            return Result.Success(account).Ensure(_ => isSuccess, error);
        }


        async Task<Result> ReleaseAccounts<TResult>(IList<IEntityLock> locksHolder, Result<TResult> result)
        {
            await _locker.ReleaseLocks(locksHolder);
            return result;
        }


        private const string LockerName = nameof(ICounterpartyAccountService);
        private readonly IAccountBalanceAuditService _auditService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}