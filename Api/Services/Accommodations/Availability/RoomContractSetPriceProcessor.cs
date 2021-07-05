using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.Money.Extensions;
using HappyTravel.Money.Helpers;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public static class RoomContractSetPriceProcessor
    {
        public static async Task<List<RoomContractSet>> ProcessPrices(List<RoomContractSet> sourceRoomContractSets, PriceProcessFunction priceProcessFunction)
        {
            var roomContractSets = new List<RoomContractSet>(sourceRoomContractSets.Count);
            foreach (var roomContractSet in sourceRoomContractSets)
            {
                var roomContractSetWithMarkup = await ProcessPrices(roomContractSet, priceProcessFunction);
                roomContractSets.Add(roomContractSetWithMarkup);
            }

            return roomContractSets;
        }


        public static async Task<RoomContractSet> ProcessPrices(RoomContractSet sourceRoomContractSet, PriceProcessFunction priceProcessFunction)
        {
            var roomContracts = new List<RoomContract>(sourceRoomContractSet.RoomContracts.Count);
            var sourceTotalPrice = sourceRoomContractSet.Rate.FinalPrice;
            var processedTotalPrice = await priceProcessFunction(sourceRoomContractSet.Rate.FinalPrice);
            
            var roomContractSetGross = ChangeProportionally(sourceRoomContractSet.Rate.Gross);
            var roomContractSetRate = new Rate(processedTotalPrice, roomContractSetGross, sourceRoomContractSet.Rate.Discounts,
                sourceRoomContractSet.Rate.Type, sourceRoomContractSet.Rate.Description);
            
            foreach (var room in sourceRoomContractSet.RoomContracts)
            {
                var dailyRates = new List<DailyRate>(room.DailyRoomRates.Count);
                foreach (var dailyRate in room.DailyRoomRates)
                {
                    var roomGross = ChangeProportionally(dailyRate.Gross);
                    var roomFinalPrice = ChangeProportionally(dailyRate.FinalPrice);

                    dailyRates.Add(BuildDailyPrice(dailyRate, roomFinalPrice, roomGross));
                }
                
                var totalPriceNet = ChangeProportionally(room.Rate.FinalPrice);
                var totalPriceGross = ChangeProportionally(room.Rate.Gross);
                var totalRate = new Rate(totalPriceNet, totalPriceGross);

                roomContracts.Add(BuildRoomContracts(room, dailyRates, totalRate));
            }

            return BuildRoomContractSet(sourceRoomContractSet, roomContractSetRate, roomContracts);


            MoneyAmount ChangeProportionally(MoneyAmount price)
            {
                if (price.Amount == 0)
                    throw new NotSupportedException($"Cannot get ratio for {price.Amount}");
                
                var totalPricePercent = price.Amount / sourceTotalPrice.Amount;
                return new MoneyAmount(processedTotalPrice.Amount * totalPricePercent, processedTotalPrice.Currency);
            }
        }


        public static async ValueTask<RoomContractSet> AlignPrices(RoomContractSet roomContractSet)
        {
            var ceiledRoomContractSet = await ProcessPrices(roomContractSet, price 
                => new ValueTask<MoneyAmount>(MoneyRounder.Ceil(price)));
            
            var finalPrice = ceiledRoomContractSet.Rate.FinalPrice;
            var roomFinalRates = ceiledRoomContractSet.RoomContracts.Select(r => r.Rate.FinalPrice).ToList();
            var (alignedFinalPrice, alignedRoomFinalPrices) = AlignAggregateValues(finalPrice, roomFinalRates);

            var gross = ceiledRoomContractSet.Rate.Gross;
            var roomGrossRateRates = ceiledRoomContractSet.RoomContracts.Select(r => r.Rate.Gross).ToList();
            var (alignedGrossPrice, alignedRoomGrossRates) = AlignAggregateValues(gross, roomGrossRateRates);

            var roomContracts = new List<RoomContract>(roomContractSet.RoomContracts.Count);
            for (var i = 0; i < ceiledRoomContractSet.RoomContracts.Count; i++)
            {
                var room = ceiledRoomContractSet.RoomContracts[i];
                var totalPriceNet = alignedRoomFinalPrices[i];
                var totalPriceGross = alignedRoomGrossRates[i];
                var totalRate = new Rate(totalPriceNet, totalPriceGross);
                
                roomContracts.Add(BuildRoomContracts(room, room.DailyRoomRates, totalRate));
            }

            var roomContractSetRate = new Rate(alignedFinalPrice, alignedGrossPrice);

            return BuildRoomContractSet(roomContractSet, roomContractSetRate, roomContracts);


            static (MoneyAmount Aggregated, List<MoneyAmount> Parts) AlignAggregateValues(MoneyAmount aggregated, List<MoneyAmount> parts)
            {
                var partsSum = new MoneyAmount(parts.Sum(p => p.Amount), aggregated.Currency);
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

                    return (new MoneyAmount(parts.Sum(p=>p.Amount), aggregated.Currency), parts);
                }
            }
        }
        
        
        private static DailyRate BuildDailyPrice(in DailyRate dailyRate, MoneyAmount roomNetTotal, MoneyAmount roomGross)
            => new DailyRate(dailyRate.FromDate, dailyRate.ToDate, roomNetTotal, roomGross, dailyRate.Type, dailyRate.Description);
        
        
        static RoomContract BuildRoomContracts(in RoomContract room, List<DailyRate> roomPrices, Rate totalPrice)
            => new RoomContract(room.BoardBasis, 
                room.MealPlan, 
                room.ContractTypeCode,
                room.IsAvailableImmediately,
                room.IsDynamic,
                room.ContractDescription,
                room.Remarks,
                roomPrices, 
                totalPrice,
                room.AdultsNumber, 
                room.ChildrenAges,
                room.Type,
                room.IsExtraBedNeeded,
                room.Deadline,
                room.IsAdvancePurchaseRate);

            
        static RoomContractSet BuildRoomContractSet(in RoomContractSet roomContractSet, in Rate roomContractSetRate, List<RoomContract> rooms)
            => new RoomContractSet(roomContractSet.Id, 
                roomContractSetRate, 
                roomContractSet.Deadline, 
                rooms, 
                roomContractSet.Tags,
                isDirectContract: roomContractSet.IsDirectContract,
                isAdvancePurchaseRate: roomContractSet.IsAdvancePurchaseRate);
    }
}