using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public class AccountPaymentProcessingService : IAccountPaymentProcessingService
    {
        public AccountPaymentProcessingService(EdoContext context,
            IEntityLocker locker,
            IAccountBalanceAuditService auditService)
        {
            _context = context;
            _locker = locker;
            _auditService = auditService;
        }


        public Task<Result> AddMoney(int accountId, PaymentData paymentData, UserInfo user)
        {
            EntityLock<PaymentAccount> accountLock = default;

            return GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Bind(LockAccount)
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(AddMoney)
                    .Map(WriteAuditLog))
                .Finally(ReleaseAccount);


            bool IsReasonProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);


            async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
            {
                accountLock = await CreateEntityLock(account);
                return Result.Success(account).Ensure(_ => accountLock.Acquired, accountLock.Error);
            }


            async Task<Result> ReleaseAccount<TResult>(Result<TResult> result)
            {
                await accountLock.DisposeAsync();
                return result;
            }


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
                await _auditService.Write(AccountEventType.Add,
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
            EntityLock<PaymentAccount> accountLock = default;

            return GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                .Bind(LockAccount)
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(ChargeMoney)
                    .Map(WriteAuditLog))
                .Finally(ReleaseAccount);


            bool IsReasonProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsBalanceSufficient(PaymentAccount account) => this.IsBalanceSufficient(account, paymentData.Amount);


            async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
            {
                accountLock = await CreateEntityLock(account);
                return Result.Success(account).Ensure(_ => accountLock.Acquired, accountLock.Error);
            }


            async Task<Result> ReleaseAccount<TResult>(Result<TResult> result)
            {
                await accountLock.DisposeAsync();
                return result;
            }


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
                await _auditService.Write(AccountEventType.Charge,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    null);

                return account;
            }
        }


        public Task<Result> AuthorizeMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            EntityLock<PaymentAccount> accountLock = default;

            return GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsBalancePositive, "Could not charge money, insufficient balance")
                .Bind(LockAccount)
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(AuthorizeMoney)
                    .Map(WriteAuditLog))
                .Finally(ReleaseAccount);


            bool IsReasonProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsBalancePositive(PaymentAccount account) => (account.Balance + account.CreditLimit).IsGreaterThan(decimal.Zero);


            async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
            {
                accountLock = await CreateEntityLock(account);
                return Result.Success(account).Ensure(_ => accountLock.Acquired, accountLock.Error);
            }


            async Task<Result> ReleaseAccount<TResult>(Result<TResult> result)
            {
                await accountLock.DisposeAsync();
                return result;
            }


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
                await _auditService.Write(AccountEventType.Authorize,
                    account.Id,
                    paymentData.Amount,
                    user,
                    eventData,
                    paymentData.ReferenceCode);

                return account;
            }
        }


        public Task<Result> CaptureMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            EntityLock<PaymentAccount> accountLock = default;

            return GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsAuthorizedSufficient, "Could not capture money, insufficient authorized balance")
                .Bind(LockAccount)
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(CaptureMoney)
                    .Map(WriteAuditLog))
                .Finally(ReleaseAccount);


            bool IsReasonProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAuthorizedSufficient(PaymentAccount account) => this.IsAuthorizedSufficient(account, paymentData.Amount);


            async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
            {
                accountLock = await CreateEntityLock(account);
                return Result.Success(account).Ensure(_ => accountLock.Acquired, accountLock.Error);
            }


            async Task<Result> ReleaseAccount<TResult>(Result<TResult> result)
            {
                await accountLock.DisposeAsync();
                return result;
            }


            async Task<PaymentAccount> CaptureMoney(PaymentAccount account)
            {
                account.AuthorizedBalance -= paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
                => WriteAuditLogWithReferenceCode(account, paymentData, AccountEventType.Capture, user);
        }


        public Task<Result> VoidMoney(int accountId, AuthorizedMoneyData paymentData, UserInfo user)
        {
            EntityLock<PaymentAccount> accountLock = default;

            return GetAccount(accountId)
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsAuthorizedSufficient, "Could not void money, insufficient authorized balance")
                .Bind(LockAccount)
                .BindWithTransaction(_context, account => Result.Success(account)
                    .Map(VoidMoney)
                    .Map(WriteAuditLog))
                .Finally(ReleaseAccount);


            bool IsReasonProvided(PaymentAccount account) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAuthorizedSufficient(PaymentAccount account) => this.IsAuthorizedSufficient(account, paymentData.Amount);


            async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
            {
                accountLock = await CreateEntityLock(account);
                return Result.Success(account).Ensure(_ => accountLock.Acquired, accountLock.Error);
            }


            async Task<Result> ReleaseAccount<TResult>(Result<TResult> result)
            {
                await accountLock.DisposeAsync();
                return result;
            }


            async Task<PaymentAccount> VoidMoney(PaymentAccount account)
            {
                account.AuthorizedBalance -= paymentData.Amount;
                account.Balance += paymentData.Amount;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return account;
            }


            Task<PaymentAccount> WriteAuditLog(PaymentAccount account)
                => WriteAuditLogWithReferenceCode(account, paymentData, AccountEventType.Void, user);
        }



        public Task<Result> TransferToChildAgency(int payerAccountId, int recipientAccountId, MoneyAmount amount, AgentContext agent)
        {
            EntityLock<PaymentAccount> payerAccountLock = default;
            EntityLock<PaymentAccount> recipientAccountLock = default;
            var user = agent.ToUserInfo();

            return Result.Success()
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .Bind(GetPayerAccount)
                .Ensure(IsAgentUsingHisAgencyAccount, "You can only transfer money from an agency you are currently using")
                .Bind(GetRecipientAccount)
                .Ensure(IsRecipientAgencyChildOfPayerAgency, "Transfers are only possible to accounts of child agencies")
                .Ensure(AreAccountsCurrenciesMatch, "Currencies of specified accounts mismatch")
                .Ensure(IsAmountCurrencyMatch, "Currency of specified amount mismatch")
                .Bind(LockPayerAccount)
                .Ensure(IsBalanceSufficient, "Could not charge money, insufficient balance")
                .Bind(LockRecipientAccount)
                .BindWithTransaction(_context, accounts => Result.Success(accounts)
                    .Map(TransferMoney)
                    .Tap(WriteAuditLog))
                .Finally(ReleaseAccounts);


            async Task<Result<PaymentAccount>> GetPayerAccount()
            {
                var (isSuccess, _, recipientAccount, _) = await GetAccount(payerAccountId);
                return isSuccess
                    ? recipientAccount
                    : Result.Failure<PaymentAccount>("Could not find payer account");
            }


            bool IsAgentUsingHisAgencyAccount(PaymentAccount payerAccount) => agent.IsUsingAgency(payerAccount.AgencyId);


            async Task<Result<(PaymentAccount payerAccount, PaymentAccount recipientAccount)>> GetRecipientAccount(PaymentAccount payerAccount)
            {
                var (isSuccess, _, recipientAccount, _) = await GetAccount(recipientAccountId);
                return isSuccess
                    ? (payerAccount, recipientAccount)
                    : Result.Failure<(PaymentAccount, PaymentAccount)>("Could not find recipient account");
            }


            bool IsAmountPositive() => amount.Amount.IsGreaterThan(decimal.Zero);


            async Task<bool> IsRecipientAgencyChildOfPayerAgency((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts)
            {
                var recipientAgency = await _context.Agencies.Where(a => a.Id == accounts.recipientAccount.AgencyId).SingleOrDefaultAsync();
                return recipientAgency.ParentId == accounts.payerAccount.AgencyId;
            }


            bool AreAccountsCurrenciesMatch((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts) =>
                accounts.payerAccount.Currency == accounts.recipientAccount.Currency;


            bool IsAmountCurrencyMatch((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts) =>
                accounts.payerAccount.Currency == amount.Currency;


            async Task<Result<(PaymentAccount, PaymentAccount)>> LockPayerAccount((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts)
            {
                payerAccountLock = await CreateEntityLock(accounts.payerAccount);
                return Result.Success(accounts).Ensure(_ => payerAccountLock.Acquired, payerAccountLock.Error);
            }


            async Task<Result<(PaymentAccount, PaymentAccount)>> LockRecipientAccount((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts)
            {
                recipientAccountLock = await CreateEntityLock(accounts.payerAccount);
                return Result.Success(accounts).Ensure(_ => recipientAccountLock.Acquired, recipientAccountLock.Error);
            }


            async Task<Result> ReleaseAccounts<TResult>(Result<TResult> result)
            {
                await payerAccountLock.DisposeAsync();
                await recipientAccountLock.DisposeAsync();

                return result;
            }


            bool IsBalanceSufficient((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts) =>
                accounts.payerAccount.Balance.IsGreaterOrEqualThan(amount.Amount);


            async Task<(PaymentAccount, PaymentAccount)> TransferMoney(
                (PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts)
            {
                accounts.payerAccount.Balance -= amount.Amount;
                _context.Update(accounts.payerAccount);

                accounts.recipientAccount.Balance += amount.Amount;
                _context.Update(accounts.recipientAccount);

                await _context.SaveChangesAsync();

                return accounts;
            }


            async Task WriteAuditLog((PaymentAccount payerAccount, PaymentAccount recipientAccount) accounts)
            {
                var counterpartyEventData = new AccountBalanceLogEventData(null, accounts.payerAccount.Balance,
                    accounts.payerAccount.CreditLimit, accounts.payerAccount.AuthorizedBalance);

                await _auditService.Write(AccountEventType.AgencyTransferToAgency, accounts.payerAccount.Id,
                    amount.Amount, user, counterpartyEventData, null);

                var agencyEventData = new AccountBalanceLogEventData(null, accounts.recipientAccount.Balance,
                    accounts.recipientAccount.CreditLimit, accounts.recipientAccount.AuthorizedBalance);

                await _auditService.Write(AccountEventType.AgencyTransferToAgency, accounts.recipientAccount.Id,
                    amount.Amount, user, agencyEventData, null);
            }
        }


        private bool IsBalanceSufficient(PaymentAccount account, decimal amount) => (account.Balance + account.CreditLimit).IsGreaterOrEqualThan(amount);


        private bool IsAuthorizedSufficient(PaymentAccount account, decimal amount) => account.AuthorizedBalance.IsGreaterOrEqualThan(amount);


        private bool AreCurrenciesMatch(PaymentAccount account, PaymentData paymentData) => account.Currency == paymentData.Currency;

        private bool AreCurrenciesMatch(PaymentAccount account, AuthorizedMoneyData paymentData) => account.Currency == paymentData.Currency;


        private async Task<Result<PaymentAccount>> GetAccount(int accountId)
        {
            var account = await _context.PaymentAccounts.SingleOrDefaultAsync(p => p.Id == accountId);
            return account == default
                ? Result.Failure<PaymentAccount>("Could not find account")
                : Result.Ok(account);
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


        private Task<EntityLock<TEntity>> CreateEntityLock<TEntity>(TEntity entity) where TEntity : IEntity =>
            _locker.CreateLock<TEntity>(entity.Id.ToString(), nameof(IAccountPaymentProcessingService));


        private readonly IAccountBalanceAuditService _auditService;
        private readonly EdoContext _context;
        private readonly IEntityLocker _locker;
    }
}