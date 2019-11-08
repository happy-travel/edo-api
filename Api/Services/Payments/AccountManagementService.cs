using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Management.AuditEvents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Customers;
using HappyTravel.Edo.Data.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class AccountManagementService : IAccountManagementService
    {
        public AccountManagementService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ILogger<AccountManagementService> logger,
            IAdministratorContext administratorContext,
            IManagementAuditService managementAuditService,
            IEntityLocker locker)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _administratorContext = administratorContext;
            _managementAuditService = managementAuditService;
            _locker = locker;
        }
        
        public async Task<Result> Create(Company company, Currencies currency)
        {
            return await Result.Ok()
                .Ensure(CompanyIsVerified, "Account creation is only available for verified companies")
                .OnSuccess(CreateAccount)
                .OnSuccess(LogSuccess)
                .OnFailure(LogFailure);
            
            bool CompanyIsVerified()
            {
                return company.State == CompanyStates.Verified;
            }

            async Task<PaymentAccount> CreateAccount()
            {
                var account = new PaymentAccount
                {
                    Balance = 0,
                    CreditLimit = 0,
                    CompanyId = company.Id,
                    Currency = currency,
                    Created = _dateTimeProvider.UtcNow()
                };
                _context.PaymentAccounts.Add(account);
                await _context.SaveChangesAsync();
                return account;
            }

            void LogSuccess(PaymentAccount account)
            {
                _logger.LogPaymentAccountCreationSuccess(
                    $"Successfully created payment account for company: '{company.Id}', account id: {account.Id}");
            }

            void LogFailure(string error)
            {
                _logger.LogPaymentAccountCreationFailed($"Failed to create account for company {company.Id}, error {error}");
            }
        }

        public Task<Result> ChangeCreditLimit(int accountId, decimal creditLimit)
        {
            return CheckPermissions()
                .Ensure(CreditLimitIsValid, "Credit limit should be greater than zero")
                .OnSuccess(GetAccount)
                .OnSuccess(LockAccount)
                .OnSuccessWithTransaction(_context, account => Result.Ok(account)
                    .OnSuccess(UpdateCreditLimit)
                    .OnSuccess(WriteAuditLog))
                .OnBoth(UnlockAccount);

            // TODO logs.

            async Task<Result<PaymentAccount>> LockAccount(PaymentAccount account)
            {
                var (isSuccess, _, error) = await _locker.Acquire<PaymentAccount>(account.Id, nameof(IPaymentProcessingService));
                return isSuccess
                    ? Result.Ok(account)
                    : Result.Fail<PaymentAccount>(error);
            }
        
            async Task<Result> UnlockAccount(Result accountResult)
            {
                await _locker.Release<PaymentAccount>(accountId);
                return accountResult;
            }

            async Task<Result<PaymentAccount>> GetAccount()
            {
                var account = await _context.PaymentAccounts.SingleOrDefaultAsync(p => p.Id == accountId);
                return account == default
                    ? Result.Fail<PaymentAccount>("Could not find payment account")
                    : Result.Ok(account);
            }

            async Task<Result> CheckPermissions()
            {
                return (await _administratorContext.HasPermission(AdministratorPermissions.CreditLimitChange)
                    ? Result.Ok()
                    : Result.Fail("No rights to change credit limit"));
            }

            bool CreditLimitIsValid()
            {
                return creditLimit >= 0;
            }

            async Task<Result<(decimal creditLimitBefore, decimal creditLimitAfter)>> UpdateCreditLimit(PaymentAccount account)
            {
                var currentCreditLimit = account.CreditLimit;
                account.CreditLimit = creditLimit;
                _context.Update(account);
                await _context.SaveChangesAsync();
                return Result.Ok((currentCreditLimit, creditLimit));
            }

            Task<Result> WriteAuditLog((decimal creditLimitBefore, decimal creditLimitAfter) limitChanges)
            {
                return _managementAuditService.Write(ManagementEventType.AccountCreditLimitChange,
                    new AccountCreditLimitChangeEvent(accountId, limitChanges.creditLimitBefore,
                        limitChanges.creditLimitAfter));
            }
        }


        public async Task<Result<PaymentAccount>> Find(int companyId, Currencies currency)
        {
            var account = await _context.PaymentAccounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.Currency == currency);
            return account == null
                ? Result.Fail<PaymentAccount>($"Cannot find payment account for company {companyId} and currency {currency}")
                : Result.Ok(account);
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AccountManagementService> _logger;
        private readonly IAdministratorContext _administratorContext;
        private readonly IManagementAuditService _managementAuditService;
        private readonly IEntityLocker _locker;
    }
}