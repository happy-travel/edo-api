using System;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedDailyRate
    {
        public DateTime FromDate { get; init; }
        public DateTime ToDate { get; init; }
        public Currencies Currency => FinalPrice.Currency;
        public string Description { get; init; }
        public CachedMoneyAmount Gross { get; init; }
        public CachedMoneyAmount FinalPrice { get; init; }
        public PriceTypes Type { get; init; }
    }
}