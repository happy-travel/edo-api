using System;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedMoneyAmount
    {
        public Decimal Amount { get; init; }
        public Currencies Currency { get; init; }
    }
}