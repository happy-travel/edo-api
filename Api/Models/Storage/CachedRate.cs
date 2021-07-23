using System.Collections.Generic;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedRate
    {
        public Currencies Currency { get; init; }
        public string Description { get; init; }
        public CachedMoneyAmount Gross { get; init; }
        public List<CachedDiscount> Discounts { get; init; }
        public CachedMoneyAmount FinalPrice { get; init; }
        public PriceTypes Type { get; init; }
    }
}