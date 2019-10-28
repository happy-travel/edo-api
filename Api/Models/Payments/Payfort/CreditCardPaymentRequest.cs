using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.Payfort
{
    public readonly struct CreditCardPaymentRequest
    {
        [JsonConstructor]
        public CreditCardPaymentRequest(decimal amount, Currencies currency, string token, string customerName, string customerEmail, string customerIp,
            string referenceCode, string languageCode, bool isStoredToken, bool isNewCard, string securityCode)
        {
            Amount = amount;
            Currency = currency;
            Token = token;
            CustomerEmail = customerEmail;
            CustomerIp = customerIp;
            ReferenceCode = referenceCode;
            LanguageCode = languageCode;
            IsStoredToken = isStoredToken;
            IsNewCard = isNewCard;
            SecurityCode = securityCode;
            CustomerName = customerName;
        }

        public decimal Amount { get; }
        public Currencies Currency { get; }
        public string Token { get; }
        public string CustomerEmail { get; }
        public string CustomerIp { get; }
        public string ReferenceCode { get; }
        public string LanguageCode { get; }
        public bool IsStoredToken { get; }
        public bool IsNewCard { get; }
        public string SecurityCode { get; }
        public string CustomerName { get; }
    }
}
