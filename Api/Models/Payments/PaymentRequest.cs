using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments
{
    /// <summary>
    ///     Payment request
    /// </summary>
    public readonly struct PaymentRequest
    {
        [JsonConstructor]
        public PaymentRequest(decimal amount, Currencies currency, string token, string referenceCode, bool isStoredToken, string securityCode)
        {
            Amount = amount;
            Currency = currency;
            Token = token;
            ReferenceCode = referenceCode;
            IsStoredToken = isStoredToken;
            SecurityCode = securityCode;
        }

        /// <summary>
        ///     Payment amount
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        ///     Currency
        /// </summary>
        public Currencies Currency { get; }

        /// <summary>
        ///     Payment token
        /// </summary>
        public string Token { get; }

        /// <summary>
        ///     Booking reference code
        /// </summary>
        public string ReferenceCode { get; }

        /// <summary>
        ///     Payment token is stored in system
        /// </summary>
        public bool IsStoredToken { get; }

        /// <summary>
        ///     Credit card security code
        /// </summary>
        public string SecurityCode { get; }
    }
}
