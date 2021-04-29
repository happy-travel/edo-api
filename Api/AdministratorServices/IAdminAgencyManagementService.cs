using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public interface IAdminAgencyManagementService
    {
        Task<Result> DeactivateAgency(int agencyId, string reason);

        Task<Result> ActivateAgency(int agencyId, string reason);

        Task<Result<AgencyInfo>> Get(int agencyId, string languageCode = LocalizationHelper.DefaultLanguageCode);

        Task<List<AgencyInfo>> GetChildAgencies(int parentAgencyId, string languageCode = LocalizationHelper.DefaultLanguageCode);

        Task<AgencyInfo> Create(RegistrationAgencyInfo agencyInfo, int counterpartyId, int? parentAgencyId);

        Task<AgencyInfo> Create(string name, int counterpartyId, string address, string billingEmail, string city, string countryCode,
            string fax, string phone, string postalCode, string website, string vatNumber, int? parentAgencyId);
    }
}
