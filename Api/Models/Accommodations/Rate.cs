using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Models;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct Rate
    {
        [JsonConstructor]
        public Rate(Dictionary<PaymentTypes, MoneyAmount> finalPrice, in MoneyAmount gross, List<Discount> discounts,
            PriceTypes type, string description)
        {
            Description = description;
            Gross = gross;
            Discounts = discounts;
            FinalPrice = finalPrice;
            Type = type;
        }
        

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
        public Dictionary<PaymentTypes, MoneyAmount> FinalPrice { get;  }
        
        /// <summary>
        ///     The price type.
        /// </summary>
        public PriceTypes Type { get; }
    }
}