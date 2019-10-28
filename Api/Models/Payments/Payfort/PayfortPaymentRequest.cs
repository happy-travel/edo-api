using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.Payfort
{
    public struct PayfortPaymentRequest
    {
        [JsonConstructor]
        public PayfortPaymentRequest(string accessCode, string merchantIdentifier, string merchantReference, string amount, string currency, string language,
            string customerEmail, string customerIp, string tokenName, string customerName, string settlementReference, string returnUrl, string rememberMe,
            string cardSecurityCode, string signature)
        {
            AccessCode = accessCode;
            MerchantIdentifier = merchantIdentifier;
            MerchantReference = merchantReference;
            Amount = amount;
            Currency = currency;
            Language = language;
            CustomerEmail = customerEmail;
            CustomerIp = customerIp;
            TokenName = tokenName;
            CustomerName = customerName;
            RememberMe = rememberMe;
            SettlementReference = settlementReference;
            ReturnUrl = returnUrl;
            CardSecurityCode = cardSecurityCode;
            Command = "PURCHASE";
            Signature = signature;
        }


        public PayfortPaymentRequest(PayfortPaymentRequest request, string signature)
        {
            this = new PayfortPaymentRequest(accessCode: request.AccessCode,
                merchantIdentifier: request.MerchantIdentifier,
                merchantReference: request.MerchantReference,
                amount: request.Amount,
                currency: request.Currency,
                customerName: request.CustomerName,
                customerEmail: request.CustomerEmail,
                customerIp: request.CustomerIp,
                language: request.Language,
                returnUrl: request.ReturnUrl,
                settlementReference: request.SettlementReference,
                tokenName: request.TokenName,
                rememberMe: request.RememberMe,
                signature: signature,
                cardSecurityCode: request.CardSecurityCode);
        }

        public string Command { get; }
        public string AccessCode { get; }
        public string MerchantIdentifier { get; }
        public string MerchantReference { get; }
        public string Amount { get; }
        public string Currency { get; }
        public string Language { get; }
        public string CustomerEmail { get; }
        public string CustomerIp { get; }
        public string TokenName { get; }
        public string Signature { get; }
        public string CustomerName { get; }
        public string RememberMe { get; }
        public string SettlementReference { get; }
        public string ReturnUrl { get; }
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string CardSecurityCode { get; }
    }
}
