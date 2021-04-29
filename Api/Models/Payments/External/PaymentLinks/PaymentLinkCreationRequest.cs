using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks
{
    public class PaymentLinkCreationRequest
    {
        [JsonConstructor]
        public PaymentLinkCreationRequest(decimal amount, string email, ServiceTypes serviceType, Currencies currency, string comment)
        {
            Amount = amount;
            Email = email;
            ServiceType = serviceType;
            Currency = currency;
            Comment = comment;
        }


        /// <summary>
        ///     Payment price.
        /// </summary>
        [Required]
        public decimal Amount { get; }

        /// <summary>
        ///     Customer e-mail.
        /// </summary>
        [Required]
        public string Email { get; }

        /// <summary>
        ///     Service type to pay for.
        /// </summary>
        public ServiceTypes ServiceType { get; }


        /// <summary>
        ///     Payment currency.
        /// </summary>
        [Required]
        public Currencies Currency { get; }

        /// <summary>
        ///     Mandatory payment comment.
        /// </summary>
        [Required]
        public string Comment { get; }
    }
}