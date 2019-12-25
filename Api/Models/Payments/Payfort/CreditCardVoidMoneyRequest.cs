using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.Payfort
{
    public readonly struct CreditCardVoidMoneyRequest
    {
        [JsonConstructor]
        public CreditCardVoidMoneyRequest(string externalId, string internalReferenceCode, string languageCode)
        {
            ExternalId = externalId;
            InternalReferenceCode = internalReferenceCode;
            LanguageCode = languageCode;
        }


        public string ExternalId { get; }
        public string InternalReferenceCode { get; }
        public string LanguageCode { get; }
    }
}
