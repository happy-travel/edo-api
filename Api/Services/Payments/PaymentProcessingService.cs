using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Payments.AuditEvents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PaymentProcessingService : IPaymentProcessingService
    {
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
        private readonly IAccountBalanceAuditService _auditService;

        public PaymentProcessingService(EdoContext context, 
            IEntityLocker locker,
            IAccountBalanceAuditService auditService)
        {
            _context = context;
            _locker = locker;
            _auditService = auditService;
        }

        public Task<Result> AddMoney(int accountId, PaymentData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(AddMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);


            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);


            async Task<PaymentAccount> AddMoney(PaymentAccount account)
            {
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.CreditLimit, account.Frozen);
                await _auditService.Write(AccountEventType.AddMoney,
                    account.Id, 
                    paymentData.Amount,
                    user, 
                    eventData);

                return account;
            }
        }

        public Task<Result> ChargeMoney(int accountId, PaymentData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .Ensure(BalanceIsSufficient, "Could not charge money, insufficient balance")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(ChargeMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);


            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;


            bool BalanceIsSufficient(PaymentAccount account) => this.BalanceIsSufficient(account, paymentData.Amount);


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);


            async Task<PaymentAccount> ChargeMoney(PaymentAccount account)
            {
                account.Balance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.CreditLimit, account.Frozen);
                await _auditService.Write(AccountEventType.ChargeMoney,
                    account.Id, 
                    paymentData.Amount,
                    user, 
                    eventData);

                return account;
            }
        }


        public Task<Result> FreezeMoney(int accountId, FrozenMoneyData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(FreezeMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);


            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);


            async Task<PaymentAccount> FreezeMoney(PaymentAccount account)
            {
                account.Frozen += paymentData.Amount;
                account.Balance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                var eventData = new AccountBalanceWithReferenceCodeLogEventData(paymentData.Reason, paymentData.ReferenceCode, account.Balance, account.CreditLimit, account.Frozen);
                await _auditService.Write(AccountEventType.FreezeMoney,
                    account.Id, 
                    paymentData.Amount,
                    user, 
                    eventData);

                return account;
            }
        }


        public Task<Result> ReleaseFrozenMoney(int accountId, FrozenMoneyData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .Ensure(FrozenIsSufficient, "Could not release frozen money, insufficient frozen balance")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(ReleaseFrozenMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);


            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;


            bool FrozenIsSufficient(PaymentAccount account) => this.FrozenIsSufficient(account, paymentData.Amount);


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);


            async Task<PaymentAccount> ReleaseFrozenMoney(PaymentAccount account)
            {
                account.Frozen -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                return WriteFreezeAuditLog(account, paymentData, AccountEventType.ReleaseFrozenMoney, user);
            }
        }


        public Task<Result> UnfreezeMoney(int accountId, FrozenMoneyData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .Ensure(FrozenIsSufficient, "Could not release frozen money, insufficient frozen balance")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(UnfreezeMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);


            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;


            bool FrozenIsSufficient(PaymentAccount account) => this.FrozenIsSufficient(account, paymentData.Amount);


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);


            async Task<PaymentAccount> UnfreezeMoney(PaymentAccount account)
            {
                account.Frozen -= paymentData.Amount;
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                return WriteFreezeAuditLog(account, paymentData, AccountEventType.UnfreezeMoney, user);
            }
        }


        private bool BalanceIsSufficient(PaymentAccount account, decimal amount) =>
            account.Balance + account.CreditLimit >= amount;


        private bool FrozenIsSufficient(PaymentAccount account, decimal amount) =>
            account.Frozen >= amount;


        private async Task<Result<PaymentAccount>> GetAccount(int accountId)
        {
            var account = await _context.PaymentAccounts.SingleOrDefaultAsync(p => p.Id == accountId);
            return account == default
                ? Result.Fail<PaymentAccount>("Could not find account")
                : Result.Ok(account);
        }


        private async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
        {
            var (isSuccess, _, error) = await _locker.Acquire<PaymentAccount>(account.Id, nameof(IPaymentProcessingService));
            return isSuccess
                ? Result.Ok(account)
                : Result.Fail<PaymentAccount>(error);
        }


        private async Task<PaymentAccount> WriteFreezeAuditLog(PaymentAccount account, FrozenMoneyData paymentData, AccountEventType eventType, UserInfo user)
        {
            var eventData = new AccountBalanceWithReferenceCodeLogEventData(paymentData.Reason, paymentData.ReferenceCode, account.Balance, account.CreditLimit, account.Frozen);
            await _auditService.Write(AccountEventType.UnfreezeMoney,
                account.Id, 
                paymentData.Amount,
                user, 
                eventData);

            return account;
        }


        private async Task<Result> UnlockAccount(Result<PaymentAccount> result, int accountId)
        {
            await _locker.Release<PaymentAccount>(accountId);
            return result;
        }
    }
}
