using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.Payfort
{
    public readonly struct CreditCardPaymentResult
    {
        [JsonConstructor]
        public CreditCardPaymentResult(string secure3d, string referenceCode, string authorizationCode, string externalCode, string expirationDate,
            string cardNumber, CreditCardPaymentStatuses status, string message, decimal amount, string merchantReference)
        {
            Secure3d = secure3d;
            ReferenceCode = referenceCode;
            AuthorizationCode = authorizationCode;
            ExternalCode = externalCode;
            ExpirationDate = expirationDate;
            CardNumber = cardNumber;
            Status = status;
            Message = message;
            Amount = amount;
            MerchantReference = merchantReference;
        }

        public string Secure3d { get; }
        public string ReferenceCode { get; }
        public string AuthorizationCode { get; }
        public string ExternalCode { get; }
        public string ExpirationDate { get; }
        public string CardNumber { get; }
        public CreditCardPaymentStatuses Status { get; }
        public string Message { get; }
        public decimal Amount { get; }
        public string MerchantReference { get; }
    }
}