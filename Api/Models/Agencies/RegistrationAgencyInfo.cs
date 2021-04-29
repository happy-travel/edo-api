using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agencies
{
    public readonly struct RegistrationAgencyInfo
    {
        [JsonConstructor]
        public RegistrationAgencyInfo(string name, string address, string billingEmail, string city,
            string countryCode, string fax, string phone, string postalCode, string website, string vatNumber)
        {
            Name = name;
            Address = address;
            BillingEmail = billingEmail;
            City = city;
            CountryCode = countryCode;
            Fax = fax;
            Phone = phone;
            PostalCode = postalCode;
            Website = website;
            VatNumber = vatNumber;
        }


        /// <summary>
        ///     Name of the agency.
        /// </summary>
        [Required]
        public string Name { get; }


        /// <summary>
        ///     Agency address.
        /// </summary>
        [Required]
        public string Address { get; }

        /// <summary>
        ///     Two-letter international country code.
        /// </summary>
        [Required]
        public string CountryCode { get; }

        /// <summary>
        ///     City name.
        /// </summary>
        [Required]
        public string City { get; }

        /// <summary>
        ///     Phone number. Only digits, length between 3 and 30.
        /// </summary>
        [Required]
        public string Phone { get; }

        /// <summary>
        ///     Fax number. Only digits, length between 3 and 30.
        /// </summary>
        public string Fax { get; }

        /// <summary>
        ///     Postal code.
        /// </summary>
        public string PostalCode { get; }

        /// <summary>
        ///     Agency site url.
        /// </summary>
        public string Website { get; }

        /// <summary>
        /// E-mail for billing operations
        /// </summary>
        public string BillingEmail { get; }

        /// <summary>
        /// Value added tax identification number
        /// </summary>
        public string VatNumber { get; }
    }
}