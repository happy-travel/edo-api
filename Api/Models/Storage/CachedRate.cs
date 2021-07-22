using System.Collections.Generic;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedRate
    {
        public Currencies Currency { get; init; }
        public string Description { get; init; }
        public MoneyAmount Gross { get; init; }
        public List<CachedDiscount> Discounts { get; init; }
        public MoneyAmount FinalPrice { get; init; }
        public PriceTypes Type { get; init; }
    }
}