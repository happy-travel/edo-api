using System.Collections.Generic;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct Rate
    {
        public Rate(in MoneyAmount finalPrice, in MoneyAmount gross, List<Discount>? discounts = null,
            PriceTypes type = PriceTypes.Room, string? description = null)
        {
            Description = description ?? string.Empty;
            Gross = gross;
            Discounts = discounts ?? new List<Discount>(0);
            FinalPrice = finalPrice;
            Type = type;
        }

        public Currencies Currency => FinalPrice.Currency;

        public string Description { get; }

        public MoneyAmount Gross { get; }

        public List<Discount>? Discounts { get; }

        public MoneyAmount FinalPrice { get; }

        public PriceTypes Type { get; }
    }
}