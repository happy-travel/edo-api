using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Invitations
{
    public readonly struct ChildAgencyInvitationInfo
    {
        [JsonConstructor]
        public ChildAgencyInvitationInfo(string name, string address, string billingEmail, string city,
            string countryCode, string countryName, string fax, string phone, string postalCode, string website, string vatNumber)
        {
            Name = name;
            Address = address;
            BillingEmail = billingEmail;
            City = city;
            CountryCode = countryCode;
            CountryName = countryName;
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
        public string Address { get; }

        /// <summary>
        ///     Two-letter international country code.
        /// </summary>
        public string CountryCode { get; }

        /// <summary>
        /// Country name.
        /// </summary>
        public string CountryName { get; }

        /// <summary>
        ///     City name.
        /// </summary>
        public string City { get; }

        /// <summary>
        ///     Phone number. Only digits, length between 3 and 30.
        /// </summary>
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