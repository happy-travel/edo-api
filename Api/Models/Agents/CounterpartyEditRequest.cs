using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct CounterpartyEditRequest
    {
        [JsonConstructor]
        public CounterpartyEditRequest(string name, PaymentTypes preferredPaymentMethod, string vatNumber)
        {
            Name = name;
            PreferredPaymentMethod = preferredPaymentMethod;
            VatNumber = vatNumber;
        }


        /// <summary>
        ///     Counterparty name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        ///     Preferable way to do payments.
        /// </summary>
        [Required]
        public PaymentTypes PreferredPaymentMethod { get; }

        /// <summary>
        /// Value added tax identification number
        /// </summary>
        public string VatNumber { get; }
    }
}