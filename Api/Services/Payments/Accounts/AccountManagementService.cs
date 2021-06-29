using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
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


        public async Task<Result> CreateForAgency(Agency agency, Currencies currency)
        {
            return await Result.Success()
                .Ensure(IsCounterpartyVerified, "Account creation is only available for verified counterparties")
                .Map(CreateAccount)
                .Tap(LogSuccess)
                .OnFailure(LogFailure);


            async Task<bool> IsCounterpartyVerified()
            {
                var counterparty = await _context.Counterparties.Where(c => c.Id == agency.CounterpartyId).SingleAsync();
                return new[] {CounterpartyStates.ReadOnly, CounterpartyStates.FullAccess}.Contains(counterparty.State);
            }


            async Task<AgencyAccount> CreateAccount()
            {
                var account = new AgencyAccount
                {
                    Balance = 0,
                    AgencyId = agency.Id,
                    Currency = Currencies.USD, // Only USD currency is supported
                    Created = _dateTimeProvider.UtcNow()
                };
                _context.AgencyAccounts.Add(account);
                await _context.SaveChangesAsync();

                return account;
            }


            void LogSuccess(AgencyAccount account)
            {
                _logger.LogAgencyAccountCreationSuccess(agency.Id, account.Id);
            }


            void LogFailure(string error)
            {
                _logger.LogAgencyAccountCreationFailed(agency.Id, error);
            }
        }


        public async Task<Result> CreateForCounterparty(Counterparty counterparty, Currencies currency)
        {
            return await Result.Success()
                .Ensure(IsCounterpartyVerified, "Account creation is only available for verified counterparties")
                .Map(CreateAccount)
                .Tap(LogSuccess)
                .OnFailure(LogFailure);

            bool IsCounterpartyVerified() => new[] {CounterpartyStates.ReadOnly, CounterpartyStates.FullAccess}.Contains(counterparty.State);


            async Task<CounterpartyAccount> CreateAccount()
            {
                var account = new CounterpartyAccount
                {
                    Balance = 0,
                    CounterpartyId = counterparty.Id,
                    Currency = Currencies.USD, // Only USD currency is supported
                    Created = _dateTimeProvider.UtcNow()
                };
                _context.CounterpartyAccounts.Add(account);
                await _context.SaveChangesAsync();

                return account;
            }


            void LogSuccess(CounterpartyAccount account)
            {
                _logger.LogCounterpartyAccountCreationSuccess(counterparty.Id, account.Id);
            }


            void LogFailure(string error)
            {
                _logger.LogCounterpartyAccountCreationFailure(counterparty.Id, error);
            }
        }


        public async Task<Result<AgencyAccount>> Get(int agencyId, Currencies currency)
        {
            var account = await _context.AgencyAccounts.FirstOrDefaultAsync(a => a.IsActive && a.AgencyId == agencyId && a.Currency == currency);
            return account == null
                ? Result.Failure<AgencyAccount>($"Cannot find account for agency '{agencyId}' and currency '{currency}'")
                : Result.Success(account);
        }


        private readonly IAdministratorContext _administratorContext;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IEntityLocker _locker;
        private readonly ILogger<AccountManagementService> _logger;
        private readonly IManagementAuditService _managementAuditService;
    }
}