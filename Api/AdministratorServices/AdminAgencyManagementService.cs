using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AdminAgencyManagementService : IAdminAgencyManagementService
    {
        public AdminAgencyManagementService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IManagementAuditService managementAuditService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _managementAuditService = managementAuditService;
        }


        public Task<Result> DeactivateAgency(int agencyId, string reason)
            => GetAgency(agencyId)
                .Ensure(_ => !string.IsNullOrWhiteSpace(reason), "Reason must not be empty")
                .BindWithTransaction(_context, agency => ChangeActivityStatus(agency, ActivityStatus.NotActive)
                    .Tap(() => WriteAgencyDeactivationToAuditLog(agencyId, reason)));


        public Task<Result> ActivateAgency(int agencyId, string reason)
            => GetAgency(agencyId)
                .Ensure(_ => !string.IsNullOrWhiteSpace(reason), "Reason must not be empty")
                .BindWithTransaction(_context, agency => ChangeActivityStatus(agency, ActivityStatus.Active)
                    .Tap(() => WriteAgencyActivationToAuditLog(agencyId, reason)));


        public async Task<Result<AgencyInfo>> Get(int agencyId, string languageCode = LocalizationHelper.DefaultLanguageCode)
        {
            var agencyInfo = await (
                    from a in _context.Agencies
                    join c in _context.Countries on a.CountryCode equals c.Code
                    join cp in _context.Counterparties on a.CounterpartyId equals cp.Id
                    where a.Id == agencyId
                    select a.ToAgencyInfo(cp.ContractKind, c.Names, languageCode))
                .SingleOrDefaultAsync();

            return agencyInfo.Equals(default)
                ? Result.Failure<AgencyInfo>("Could not find specified agency")
                : agencyInfo;
        }


        public Task<List<AgencyInfo>> GetChildAgencies(int parentAgencyId, string languageCode = LocalizationHelper.DefaultLanguageCode)
            => (
                    from a in _context.Agencies
                    join c in _context.Countries on a.CountryCode equals c.Code
                    join cp in _context.Counterparties on a.CounterpartyId equals cp.Id
                    where a.ParentId == parentAgencyId
                    select a.ToAgencyInfo(cp.ContractKind, c.Names, languageCode))
                .ToListAsync();


        public Task<Result> ChangeActivityStatus(Agency agency, ActivityStatus status)
        {
            var convertedStatus = ConvertToDbStatus(status);
            if (convertedStatus == agency.IsActive)
                return Task.FromResult(Result.Success());

            return ChangeAgencyActivityStatus()
                .Tap(ChangeAgentsActivityStatus)
                .Tap(ChangeAgencyAccountsActivityStatus)
                .Tap(ChangeChildAgenciesActivityStatus);


            async Task<Result> ChangeAgencyActivityStatus()
            {
                agency.IsActive = convertedStatus;
                agency.Modified = _dateTimeProvider.UtcNow();

                _context.Update(agency);
                await _context.SaveChangesAsync();
                return Result.Success();
            }


            async Task ChangeAgencyAccountsActivityStatus()
            {
                var agencyAccounts = await _context.AgencyAccounts
                    .Where(ac => ac.AgencyId == agency.Id)
                    .ToListAsync();

                foreach (var account in agencyAccounts)
                    account.IsActive = convertedStatus;

                _context.UpdateRange(agencyAccounts);
                await _context.SaveChangesAsync();
            }


            async Task ChangeAgentsActivityStatus()
            {
                var agencyRelations = await _context.AgentAgencyRelations
                    .Where(ar => ar.AgencyId == agency.Id)
                    .ToListAsync();

                foreach (var relation in agencyRelations)
                    relation.IsActive = convertedStatus;

                _context.UpdateRange(agencyRelations);
                await _context.SaveChangesAsync();
            }


            async Task ChangeChildAgenciesActivityStatus()
            {
                var childAgencies = await _context.Agencies
                    .Where(a => a.ParentId == agency.Id && a.IsActive != convertedStatus)
                    .ToListAsync();

                foreach (var childAgency in childAgencies)
                    await ChangeActivityStatus(childAgency, status);
            }
        }


        public async Task<AgencyInfo> Create(RegistrationAgencyInfo agencyInfo, int counterpartyId, int? parentAgencyId)
            => await Create(agencyInfo.Name, counterpartyId, agencyInfo.Address, agencyInfo.BillingEmail, agencyInfo.City,
                agencyInfo.CountryCode, agencyInfo.Fax, agencyInfo.Phone, agencyInfo.PostalCode, agencyInfo.Website, agencyInfo.VatNumber, parentAgencyId);
        
        
        public async Task<AgencyInfo> Create(string name, int counterpartyId, string address, string billingEmail, string city, string countryCode,
            string fax, string phone, string postalCode, string website, string vatNumber, int? parentAgencyId)
        {
            var now = _dateTimeProvider.UtcNow();
            var agency = new Agency
            {
                Name = name,
                CounterpartyId = counterpartyId,
                Created = now,
                Modified = now,
                ParentId = parentAgencyId,
                Address = address,
                BillingEmail = billingEmail,
                City = city,
                CountryCode = countryCode,
                Fax = fax,
                Phone = phone,
                PostalCode = postalCode,
                Website = website,
                VatNumber = vatNumber,
                // Hardcode because we only support USD
                PreferredCurrency = Currencies.USD,
            };
            _context.Agencies.Add(agency);

            await _context.SaveChangesAsync();
            return (await Get(agency.Id)).Value;
        }


        private async Task<Result<Agency>> GetAgency(int agencyId)
        {
            var agency = await _context.Agencies.FirstOrDefaultAsync(ag => ag.Id == agencyId);
            if (agency == null)
                return Result.Failure<Agency>("Could not find agency with specified id");

            return Result.Success(agency);
        }


        private Task WriteAgencyDeactivationToAuditLog(int agencyId, string reason)
            => _managementAuditService.Write(ManagementEventType.AgencyDeactivation,
                new AgencyActivityStatusChangeEventData(agencyId, reason));


        private Task WriteAgencyActivationToAuditLog(int agencyId, string reason)
            => _managementAuditService.Write(ManagementEventType.AgencyActivation,
                new AgencyActivityStatusChangeEventData(agencyId, reason));


        private bool ConvertToDbStatus(ActivityStatus status) => status == ActivityStatus.Active;


        private readonly IManagementAuditService _managementAuditService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly EdoContext _context;
    }
}
