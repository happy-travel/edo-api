using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.AdministratorServices.Models;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Locations;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class CounterpartyManagementService : ICounterpartyManagementService
    {
        public CounterpartyManagementService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IManagementAuditService managementAuditService,
            IAccountManagementService accountManagementService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _managementAuditService = managementAuditService;
            _accountManagementService = accountManagementService;
        }


        public async Task<Result<CounterpartyInfo>> Get(int counterpartyId, string languageCode)
        {
            var counterpartyData = await (from cp in _context.Counterparties
                join c in _context.Countries
                    on cp.CountryCode equals c.Code
                where cp.Id == counterpartyId
                select new
                {
                    Counterparty = cp,
                    Country = c
                }).SingleOrDefaultAsync();
            if (counterpartyData == default)
                return Result.Failure<CounterpartyInfo>("Could not find counterparty with specified id");

            return ToCounterpartyInfo(counterpartyData.Counterparty, counterpartyData.Country, languageCode);
        }


        public async Task<List<CounterpartyInfo>> Get(string languageCode)
        {
            var counterparties = await (from cp in _context.Counterparties
                join c in _context.Countries
                    on cp.CountryCode equals c.Code
                select new
                {
                    Counterparty = cp,
                    Country = c
                }).ToListAsync();

            return counterparties.Select(c => ToCounterpartyInfo(c.Counterparty, c.Country, languageCode)).ToList();
        }


        public Task<List<CounterpartyPrediction>> GetCounterpartiesPredictions(string query)
            => (from c in _context.Counterparties
                    join ag in _context.Agencies on c.Id equals ag.CounterpartyId
                    join ar in _context.AgentAgencyRelations on ag.Id equals ar.AgencyId
                    join a in _context.Agents on ar.AgentId equals a.Id
                    where c.IsActive
                        && ar.IsActive
                        && ar.Type == AgentAgencyRelationTypes.Master
                        && c.State == CounterpartyStates.FullAccess
                        && !string.IsNullOrEmpty(c.Name) && c.Name.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(a.FirstName) && a.FirstName.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(a.LastName) && a.LastName.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(c.BillingEmail) && c.BillingEmail.ToLower().StartsWith(query.ToLower())
                            || !string.IsNullOrEmpty(a.Email) && a.Email.ToLower().StartsWith(query.ToLower())
                    select new CounterpartyPrediction(c.Id, c.Name, a.FirstName + " " + a.LastName, c.BillingEmail ?? a.Email))
                .Distinct()
                .ToListAsync();


        public Task<Result<List<AgencyInfo>>> GetAllCounterpartyAgencies(int counterpartyId)
        {
            return GetCounterparty(counterpartyId)
                .Map(counterparty => GetAgencies());


            Task<List<AgencyInfo>> GetAgencies()
                => _context.Agencies.Where(a => a.CounterpartyId == counterpartyId)
                    .Select(b => new AgencyInfo(b.Name, b.Id)).ToListAsync();
        }


        public Task<Result<CounterpartyInfo>> Update(CounterpartyEditRequest changedCounterpartyInfo, int counterpartyId, string languageCode)
        {
            return GetCounterparty(counterpartyId)
                .Bind(UpdateCounterparty);


            async Task<Result<CounterpartyInfo>> UpdateCounterparty(Counterparty counterpartyToUpdate)
            {
                var (_, isFailure, error) = CounterpartyValidator.Validate(changedCounterpartyInfo);
                if (isFailure)
                    return Result.Failure<CounterpartyInfo>(error);

                counterpartyToUpdate.Address = changedCounterpartyInfo.Address;
                counterpartyToUpdate.City = changedCounterpartyInfo.City;
                counterpartyToUpdate.CountryCode = changedCounterpartyInfo.CountryCode;
                counterpartyToUpdate.Fax = changedCounterpartyInfo.Fax;
                counterpartyToUpdate.Name = changedCounterpartyInfo.Name;
                counterpartyToUpdate.Phone = changedCounterpartyInfo.Phone;
                counterpartyToUpdate.Website = changedCounterpartyInfo.Website;
                counterpartyToUpdate.PostalCode = changedCounterpartyInfo.PostalCode;
                counterpartyToUpdate.PreferredCurrency = changedCounterpartyInfo.PreferredCurrency;
                counterpartyToUpdate.PreferredPaymentMethod = changedCounterpartyInfo.PreferredPaymentMethod;
                counterpartyToUpdate.Updated = _dateTimeProvider.UtcNow();
                counterpartyToUpdate.VatNumber = changedCounterpartyInfo.VatNumber;
                counterpartyToUpdate.BillingEmail = changedCounterpartyInfo.BillingEmail;

                _context.Counterparties.Update(counterpartyToUpdate);
                await _context.SaveChangesAsync();

                return await Get(counterpartyId, languageCode);
            }
        }


        public async Task<Result> Verify(int counterpartyId, CounterpartyStates state, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure("Verification reason cannot be empty");

            switch (state)
            {
                case CounterpartyStates.FullAccess:
                    return await VerifyAsFullyAccessed(counterpartyId, reason);
                case CounterpartyStates.ReadOnly:
                    return await VerifyAsReadOnly(counterpartyId, reason);
                case CounterpartyStates.DeclinedVerification:
                    return await DeclineVerification(counterpartyId, reason);
                default:
                    return Result.Failure("Invalid verification state");
            }
        }


        // This method is the same with CounterpartyService.GetCounterparty,
        // because administrator services in the future will be replaced to another application
        private async Task<Result<Counterparty>> GetCounterparty(int counterpartyId)
        {
            var counterparty = await _context.Counterparties.SingleOrDefaultAsync(c => c.Id == counterpartyId);

            if (counterparty == null)
                return Result.Failure<Counterparty>("Could not find counterparty with specified id");

            return Result.Success(counterparty);
        }


        private async Task<Result> VerifyAsFullyAccessed(int counterpartyId, string verificationReason)
        {
            return await GetCounterparty(counterpartyId)
                .Ensure(c => c.State == CounterpartyStates.ReadOnly,
                    "Verification as fully accessed is only available for counterparties that were verified as read-only earlier")
                .Tap(c => SetVerificationState(c, CounterpartyStates.FullAccess, verificationReason))
                .Tap(() => WriteVerificationToAuditLog(counterpartyId, verificationReason));
        }


        private Task<Result> VerifyAsReadOnly(int counterpartyId, string verificationReason)
        {
            return GetCounterparty(counterpartyId)
                .Ensure(c => c.State == CounterpartyStates.PendingVerification,
                    "Verification as read-only is only available for counterparties that are in pending verification step")
                .BindWithTransaction(_context, c => SetReadOnlyVerificationState(c)
                    .Bind(CreateAccountForCounterparty)
                    .Bind(() => CreateAccountsForAgencies(c)))
                .Tap(() => WriteVerificationToAuditLog(counterpartyId, verificationReason));


            async Task<Result<Counterparty>> SetReadOnlyVerificationState(Counterparty counterparty)
            {
                await SetVerificationState(counterparty, CounterpartyStates.ReadOnly, verificationReason);
                return counterparty;
            }


            Task<Result> CreateAccountForCounterparty(Counterparty counterparty)
                => _accountManagementService
                    .CreateForCounterparty(counterparty, counterparty.PreferredCurrency);


            async Task<Result> CreateAccountsForAgencies(Counterparty counterparty)
            {
                var agencies = await _context.Agencies.Where(a => a.CounterpartyId == counterpartyId).ToListAsync();

                foreach (var agency in agencies)
                {
                    var (_, isFailure) = await _accountManagementService.CreateForAgency(agency, counterparty.PreferredCurrency);
                    if (isFailure)
                        return Result.Failure("Error while creating accounts for agencies");
                }

                return Result.Success();
            }
        }


        private async Task<Result> DeclineVerification(int counterpartyId, string verificationReason)
        {
            return await GetCounterparty(counterpartyId)
                .Ensure(c => c.State == CounterpartyStates.PendingVerification,
                    "Verification failure is only available for counterparties that are in a pending state")
                .Tap(c => SetVerificationState(c, CounterpartyStates.DeclinedVerification, verificationReason))
                .Tap(() => WriteVerificationToAuditLog(counterpartyId, verificationReason));
        }


        public Task<Result> DeactivateCounterparty(int counterpartyId, string reason)
            => GetCounterparty(counterpartyId)
                .Ensure(_ => !string.IsNullOrWhiteSpace(reason), "Reason must not be empty")
                .BindWithTransaction(_context, counterparty => ChangeActivityStatus(counterparty, ActivityStatus.NotActive)
                    .Tap(() => WriteCounterpartyDeactivationToAuditLog(counterpartyId, reason)));


        public Task<Result> ActivateCounterparty(int counterpartyId, string reason)
            => GetCounterparty(counterpartyId)
                .Ensure(_ => !string.IsNullOrWhiteSpace(reason), "Reason must not be empty")
                .BindWithTransaction(_context, counterparty => ChangeActivityStatus(counterparty, ActivityStatus.Active)
                    .Tap(() => WriteCounterpartyActivationToAuditLog(counterpartyId, reason)));


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


        private async Task<Result<Agency>> GetAgency(int agencyId)
        {
            var agency = await _context.Agencies.FirstOrDefaultAsync(ag => ag.Id == agencyId);
            if (agency == null)
                return Result.Failure<Agency>("Could not find agency with specified id");

            return Result.Success(agency);
        }


        private Task<Result> ChangeActivityStatus(Counterparty counterparty, ActivityStatus status)
        {
            var convertedStatus = ConvertToDbStatus(status);
            if (convertedStatus == counterparty.IsActive)
                return Task.FromResult(Result.Success());

            return ChangeCounterpartyActivityStatus()
                .Tap(ChangeCounterpartyAccountsActivityStatus)
                .Tap(ChangeCounterpartyAgenciesActivityStatus);


            async Task<Result> ChangeCounterpartyActivityStatus()
            {
                counterparty.IsActive = convertedStatus;
                counterparty.Updated = _dateTimeProvider.UtcNow();

                _context.Update(counterparty);
                await _context.SaveChangesAsync();
                return Result.Success();
            }


            async Task ChangeCounterpartyAccountsActivityStatus()
            {
                var counterpartyAccounts = await _context.CounterpartyAccounts
                    .Where(c => c.CounterpartyId == counterparty.Id)
                    .ToListAsync();

                foreach (var account in counterpartyAccounts)
                    account.IsActive = convertedStatus;

                _context.UpdateRange(counterpartyAccounts);
                await _context.SaveChangesAsync();
            }


            async Task ChangeCounterpartyAgenciesActivityStatus()
            {
                var agencies = await _context.Agencies
                    .Where(ag => ag.CounterpartyId == counterparty.Id && ag.IsActive != convertedStatus)
                    .ToListAsync();

                foreach (var agency in agencies)
                    await ChangeActivityStatus(agency, status);
            }
        }


        private Task<Result> ChangeActivityStatus(Agency agency, ActivityStatus status)
        {
            var convertedStatus = ConvertToDbStatus(status);
            if (convertedStatus == agency.IsActive)
                return Task.FromResult(Result.Success());

            return ChangeAgencyActivityStatus()
                .Tap(ChangeAgentsActivityStatus)
                .Tap(ChangeAgencyAccountsActivityStatus)
                .Tap(ChangeChildAgenciesActivityStatus)
                .Tap(ChangeCounterpartyActivityStatusIfNeeded);


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


            async Task ChangeCounterpartyActivityStatusIfNeeded()
            {
                if (agency.ParentId == null)
                {
                    var counterparty = await _context.Counterparties
                        .Where(c => c.Id == agency.CounterpartyId)
                        .SingleAsync();

                    if (counterparty.IsActive != convertedStatus)
                        await ChangeActivityStatus(counterparty, status);
                }
            }
        }


        private Task SetVerificationState(Counterparty counterparty, CounterpartyStates state, string verificationReason)
        {
            var now = _dateTimeProvider.UtcNow();
            string reason;
            if (string.IsNullOrEmpty(counterparty.VerificationReason))
                reason = verificationReason;
            else
                reason = counterparty.VerificationReason + Environment.NewLine + verificationReason;

            counterparty.State = state;
            counterparty.VerificationReason = reason;
            counterparty.Verified = now;
            counterparty.Updated = now;
            _context.Update(counterparty);

            return _context.SaveChangesAsync();
        }


        private Task WriteVerificationToAuditLog(int counterpartyId, string verificationReason)
            => _managementAuditService.Write(ManagementEventType.CounterpartyVerification,
                new CounterpartyVerifiedAuditEventData(counterpartyId, verificationReason));


        private Task WriteCounterpartyDeactivationToAuditLog(int counterpartyId, string reason)
            => _managementAuditService.Write(ManagementEventType.CounterpartyDeactivation,
                new CounterpartyActivityStatusChangeEventData(counterpartyId, reason));


        private Task WriteCounterpartyActivationToAuditLog(int counterpartyId, string reason)
            => _managementAuditService.Write(ManagementEventType.CounterpartyActivation,
                new CounterpartyActivityStatusChangeEventData(counterpartyId, reason));


        private Task WriteAgencyDeactivationToAuditLog(int agencyId, string reason)
            => _managementAuditService.Write(ManagementEventType.AgencyDeactivation,
                new AgencyActivityStatusChangeEventData(agencyId, reason));


        private Task WriteAgencyActivationToAuditLog(int agencyId, string reason)
            => _managementAuditService.Write(ManagementEventType.AgencyActivation,
                new AgencyActivityStatusChangeEventData(agencyId, reason));


        private static CounterpartyInfo ToCounterpartyInfo(Counterparty counterparty, Country country, string languageCode)
            => new CounterpartyInfo(
                counterparty.Id,
                counterparty.Name,
                counterparty.Address,
                counterparty.CountryCode,
                LocalizationHelper.GetValueFromSerializedString(country.Names, languageCode),
                counterparty.City,
                counterparty.Phone,
                counterparty.Fax,
                counterparty.PostalCode,
                counterparty.PreferredCurrency,
                counterparty.PreferredPaymentMethod,
                counterparty.Website,
                counterparty.VatNumber,
                counterparty.BillingEmail);


        private bool ConvertToDbStatus(ActivityStatus status) => status == ActivityStatus.Active;


        private readonly IAccountManagementService _accountManagementService;
        private readonly IManagementAuditService _managementAuditService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly EdoContext _context;
    }
}