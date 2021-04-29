using System;
using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct RegistrationCounterpartyInfo
    {
        [JsonConstructor]
        public RegistrationCounterpartyInfo(string name, string legalAddress, PaymentTypes preferredPaymentMethod)
        {
            Name = name;
            LegalAddress = legalAddress;
            PreferredPaymentMethod = preferredPaymentMethod;
        }

        /// <summary>
        ///     Counterparty name.
        /// </summary>
        [Required]
        public string Name { get; }

        /// <summary>
        ///     Agency address.
        /// </summary>
        [Required]
        public string LegalAddress { get; }

        /// <summary>
        ///     Preferable way to do payments.
        /// </summary>
        [Required]
        public PaymentTypes PreferredPaymentMethod { get; }
    }
}