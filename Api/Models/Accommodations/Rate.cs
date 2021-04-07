using System.Collections.Generic;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct Rate
    {
        [JsonConstructor]
        public Rate(in MoneyAmount finalPrice, in MoneyAmount creditCardPrice, in MoneyAmount gross, List<Discount> discounts,
            PriceTypes type, string description)
        {
            Description = description;
            Gross = gross;
            Discounts = discounts;
            FinalPrice = finalPrice;
            CreditCardPrice = creditCardPrice;
            Type = type;
            Currency = finalPrice.Currency;
        }
        
        /// <summary>
        ///     The price currency.
        /// </summary>
        public Currencies Currency { get; }

        /// <summary>
        ///     The price description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///     The gross price of a service. This is just <b>a reference</b> value.
        /// </summary>
        public MoneyAmount Gross { get; }

        /// <summary>
        ///     The list of available discounts.
        /// </summary>
        public List<Discount> Discounts { get; }

        /// <summary>
        ///     The final and total net price of a service. This is <b>the actual</b> value of a price.
        /// </summary>
        public MoneyAmount FinalPrice { get; }
        
        
        /// <summary>
        ///     The final price for credit card payments.
        /// </summary>
        public MoneyAmount CreditCardPrice { get; }

        /// <summary>
        ///     The price type.
        /// </summary>
        public PriceTypes Type { get; }
    }
}