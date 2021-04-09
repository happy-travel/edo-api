using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AgencyAccountService : IAgencyAccountService
    {
        public AgencyAccountService(EdoContext context, IEntityLocker locker, IAccountBalanceAuditService auditService)
        {
            _context = context;
            _locker = locker;
            _auditService = auditService;
        }


        public async Task<Result> IncreaseManually(int agencyAccountId, PaymentData paymentData, ApiCaller apiCaller)
        {
            return await GetAgencyAccount(agencyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .BindWithLock(_locker, a => Result.Success(a)
                    .BindWithTransaction(_context, accounts => Result.Success(accounts)
                        .Map(Increase)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(AgencyAccount _) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAmountPositive(AgencyAccount _) => paymentData.Amount.IsGreaterThan(decimal.Zero);


            async Task<AgencyAccount> WriteAuditLog(AgencyAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance);
                await _auditService.Write(AccountEventType.ManualIncrease,
                    account.Id,
                    paymentData.Amount,
                    apiCaller,
                    eventData,
                    null);

                return account;
            }


            async Task<AgencyAccount> Increase(AgencyAccount agencyAccount)
            {
                agencyAccount.Balance += paymentData.Amount;
                _context.Update(agencyAccount);
                await _context.SaveChangesAsync();
                return agencyAccount;
            }
        }


        public async Task<Result> DecreaseManually(int agencyAccountId, PaymentData paymentData, ApiCaller apiCaller)
        {
            return await GetAgencyAccount(agencyAccountId)
                .Ensure(a => AreCurrenciesMatch(a, paymentData), "Account and payment currency mismatch")
                .Ensure(IsReasonProvided, "Payment reason cannot be empty")
                .Ensure(IsAmountPositive, "Payment amount must be a positive number")
                .BindWithLock(_locker, a => Result.Success(a)
                    .BindWithTransaction(_context, accounts => Result.Success(accounts)
                        .Map(Decrease)
                        .Map(WriteAuditLog)));

            bool IsReasonProvided(AgencyAccount _) => !string.IsNullOrEmpty(paymentData.Reason);

            bool IsAmountPositive(AgencyAccount _) => paymentData.Amount.IsGreaterThan(decimal.Zero);


            async Task<AgencyAccount> WriteAuditLog(AgencyAccount account)
            {
                var eventData = new AccountBalanceLogEventData(paymentData.Reason, account.Balance);
                await _auditService.Write(AccountEventType.ManualDecrease,
                    account.Id,
                    paymentData.Amount,
                    apiCaller,
                    eventData,
                    null);

                return account;
            }


            async Task<AgencyAccount> Decrease(AgencyAccount agencyAccount)
            {
                agencyAccount.Balance -= paymentData.Amount;
                _context.Update(agencyAccount);
                await _context.SaveChangesAsync();
                return agencyAccount;
            }
        }


        private async Task<Result<AgencyAccount>> GetAgencyAccount(int agencyAccountId)
        {
            var agencyAccount = await _context.AgencyAccounts
                .SingleOrDefaultAsync(ac => ac.Id == agencyAccountId);

            if (agencyAccount == default)
                return Result.Failure<AgencyAccount>("Could not found an account.");

            return Result.Success(agencyAccount);
        }


        private bool AreCurrenciesMatch(AgencyAccount account, PaymentData paymentData) => account.Currency == paymentData.Currency;


        private readonly IAccountBalanceAuditService _auditService;
        private readonly IEntityLocker _locker;
        private readonly EdoContext _context;
    }
}