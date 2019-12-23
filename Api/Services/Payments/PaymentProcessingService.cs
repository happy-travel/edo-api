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
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.CreditLimit, account.AuthorizedBalance);
                await _auditService.Write(AccountEventType.AddMoney,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    null);

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


            async Task<PaymentAccount> ChargeMoney(PaymentAccount account)
            {
                account.Balance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.CreditLimit, account.AuthorizedBalance);
                await _auditService.Write(AccountEventType.ChargeMoney,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    null);

                return account;
            }


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);
        }


        public Task<Result> AuthorizeMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(AuthorizeMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);

            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;


            async Task<PaymentAccount> AuthorizeMoney(PaymentAccount account)
            {
                account.AuthorizedBalance += paymentData.Amount;
                account.Balance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            async Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.CreditLimit, account.AuthorizedBalance);
                await _auditService.Write(AccountEventType.AuthorizeMoney,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    paymentData.ReferenceCode);

                return account;
            }


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);
        }


        public Task<Result> CaptureMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .Ensure(AuthorizedIsSufficient, "Could not capture money, insufficient authorized balance")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(CaptureMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);

            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;

            bool AuthorizedIsSufficient(PaymentAccount account) => this.AuthorizedIsSufficient(account, paymentData.Amount);


            async Task<PaymentAccount> CaptureMoney(PaymentAccount account)
            {
                account.AuthorizedBalance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
                => WriteAuditLogWithReferenceCode(account, paymentData, AccountEventType.CaptureMoney, user);


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);
        }


        public Task<Result> VoidMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            return GetAccount(accountId)
                .Ensure(ReasonIsProvided, "Payment reason cannot be empty")
                .Ensure(CurrencyIsCorrect, "Account and payment currency mismatch")
                .Ensure(AuthorizedIsSufficient, "Could not void money, insufficient authorized balance")
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(VoidMoney)
                    .OnSuccess(WriteAuditLog)
                )
                .OnBoth(UnlockAccount);

            bool ReasonIsProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool CurrencyIsCorrect(PaymentAccount account) => account.Currency == paymentData.Currency;

            bool AuthorizedIsSufficient(PaymentAccount account) => this.AuthorizedIsSufficient(account, paymentData.Amount);


            async Task<PaymentAccount> VoidMoney(PaymentAccount account)
            {
                account.AuthorizedBalance -= paymentData.Amount;
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
                => WriteAuditLogWithReferenceCode(account, paymentData, AccountEventType.VoidMoney, user);


            Task<Result> UnlockAccount(Result<PaymentAccount> result) => this.UnlockAccount(result, accountId);
        }


        private bool BalanceIsSufficient(PaymentAccount account, decimal amount) => account.Balance + account.CreditLimit >= amount;


        private bool AuthorizedIsSufficient(PaymentAccount account, decimal amount) => account.AuthorizedBalance >= amount;


        private async Task<Result<PaymentAccount>> GetAccount(int accountId)
        {
            var account = await _context.PaymentAccounts.SingleOrDefaultAsync(p => p.Id == accountId);
            return account == default
                ? Result.Fail<PaymentAccount>("Could not find account")
                : Result.Ok(account);
        }


        private async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
        {
            var (isSuccess, _, error) = await _locker.Acquire<PaymentAccount>(account.Id.ToString(), nameof(IPaymentProcessingService));
            return isSuccess
                ? Result.Ok(account)
                : Result.Fail<PaymentAccount>(error);
        }


        private async Task<PaymentAccount> WriteAuditLogWithReferenceCode(PaymentAccount account, AuthorizedMoneyData paymentData, AccountEventType eventType,
            UserInfo user)
        {
            var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance, account.CreditLimit, account.AuthorizedBalance);
            await _auditService.Write(eventType,
                account.Id,
                paymentData.Amount,
                user,
                eventData,
                paymentData.ReferenceCode);

            return account;
        }


        private async Task<Result> UnlockAccount(Result<PaymentAccount> result, int accountId)
        {
            await _locker.Release<PaymentAccount>(accountId.ToString());
            return result;
        }


        private readonly IAccountBalanceAuditService _auditService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}