using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Data.Agents;

namespace HappyTravel.Edo.Api.Extensions
{
    public static class AgencyInfoExtensions
    {
        public static AgencyInfo ToAgencyInfo(this Agency agency, CounterpartyContractKind? contractKind, string countryNames, string languageCode)
            => new AgencyInfo(
                agency.Name,
                agency.Id,
                agency.CounterpartyId,
                agency.Address,
                agency.BillingEmail,
                agency.City,
                agency.CountryCode,
                LocalizationHelper.GetValueFromSerializedString(countryNames, languageCode),
                agency.Fax,
                agency.Phone,
                agency.PostalCode,
                agency.Website,
                agency.VatNumber,
                BookingPaymentTypesHelper.GetDefaultPaymentType(contractKind));


        public static RegistrationAgencyInfo ToRegistrationAgencyInfo(this RegistrationRootAgencyInfo info, string name)
            => new RegistrationAgencyInfo(
                name,
                info.Address,
                info.BillingEmail,
                info.City,
                info.CountryCode,
                info.Fax,
                info.Phone,
                info.PostalCode,
                info.Website,
                info.VatNumber);
    }
}
