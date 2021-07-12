using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Money.Extensions;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public static class PriceAligner
    {
        public static (MoneyAmount Aggregated, List<MoneyAmount> Parts) AlignAggregateValues(MoneyAmount aggregated, List<MoneyAmount> parts)
        {
            var partsSum = new MoneyAmount(parts.Sum(p => p.Amount), aggregated.Currency);
            if (Math.Abs(aggregated.Amount - partsSum.Amount) > MaxSupportedAggregatePriceDifferenceThreshold)
                throw new NotSupportedException(
                    $"Aggregated value {aggregated.Amount} differs from aggregate sum {partsSum.Amount} for mor than allowed threshold {MaxSupportedAggregatePriceDifferenceThreshold}");

            return aggregated switch
            {
                _ when aggregated == partsSum => (aggregated, parts),
                _ when aggregated < partsSum => (partsSum, parts),
                _ when aggregated > partsSum => Align(aggregated, parts),
                _ => throw new ArgumentOutOfRangeException(nameof(aggregated), aggregated, null)
            };


            static (MoneyAmount Aggregated, List<MoneyAmount> Parts) Align(MoneyAmount aggregated, List<MoneyAmount> parts)
            {
                var changeStep = 1 / aggregated.Currency.GetDecimalDigitsCount();
                while (parts.Sum(p => p.Amount) < aggregated.Amount)
                {
                    parts = parts
                        .Select(p => new MoneyAmount(p.Amount + changeStep, p.Currency))
                        .ToList();
                }

                return (new MoneyAmount(parts.Sum(p => p.Amount), aggregated.Currency), parts);
            }
        }
        
        
        private const decimal MaxSupportedAggregatePriceDifferenceThreshold = (decimal) 0.5;
    }
}