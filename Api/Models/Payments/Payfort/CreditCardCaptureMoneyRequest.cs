using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.Payfort
{
    public readonly struct CreditCardCaptureMoneyRequest
    {
        [JsonConstructor]
        public CreditCardCaptureMoneyRequest(decimal amount, Currencies currency, string externalId, string internalReferenceCode, string languageCode)
        {
            Amount = amount;
            Currency = currency;
            ExternalId = externalId;
            InternalReferenceCode = internalReferenceCode;
            LanguageCode = languageCode;
        }


        public decimal Amount { get; }
        public Currencies Currency { get; }
        public string ExternalId { get; }
        public string InternalReferenceCode { get; }
        public string LanguageCode { get; }
    }
}
