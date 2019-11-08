using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments
{
    public readonly struct FreezePaymentData
    {
        [JsonConstructor]
        public FreezePaymentData(decimal amount, Currencies currency, string referenceCode, string reason)
        {
            Amount = amount;
            Currency = currency;
            ReferenceCode = referenceCode;
            Reason = reason;
        }
        
        /// <summary>
        /// Money amount.
        /// </summary>
        [Required]
        public decimal Amount { get; }

        /// <summary>
        /// Payment currency.
        /// </summary>
        [Required]
        public Currencies Currency { get; }

        /// <summary>
        /// Reference code.
        /// </summary>
        public string ReferenceCode { get; }

        /// <summary>
        /// Reason for payment.
        /// </summary>
        [Required]
        public string Reason { get; }
    }
}
