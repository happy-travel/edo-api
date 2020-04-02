using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public static class AvailabilityResultsExtensions
    {
        public static async ValueTask<CombinedAvailabilityDetails> ProcessPrices(this CombinedAvailabilityDetails source,
            PriceProcessFunction processFunction)
        {
            var resultsWithMarkup = new List<ProviderData<AvailabilityResult>>(source.Results.Count);
            foreach (var supplierResponse in source.Results)
            {
                var supplierRoomContractSets = supplierResponse.Data.Agreements;
                var roomContractSetsWithMarkup = await ProcessRoomContractSetsPrices(supplierRoomContractSets, processFunction);
                var responseWithMarkup = ProviderData.Create(supplierResponse.Source,
                    new AvailabilityResult(supplierResponse.Data, roomContractSetsWithMarkup));

                resultsWithMarkup.Add(responseWithMarkup);
            }

            return new CombinedAvailabilityDetails(source, resultsWithMarkup);
        }


        public static async ValueTask<SingleAccommodationAvailabilityDetails> ProcessPrices(this SingleAccommodationAvailabilityDetails source,
            PriceProcessFunction processFunction)
        {
            var roomContractSets = await ProcessRoomContractSetsPrices(source.Agreements, processFunction);
            return new SingleAccommodationAvailabilityDetails(source.AvailabilityId,
                source.CheckInDate,
                source.CheckOutDate,
                source.NumberOfNights,
                source.AccommodationDetails,
                roomContractSets);
        }


        public static async ValueTask<SingleAccommodationAvailabilityDetailsWithDeadline?> ProcessPrices(
            this SingleAccommodationAvailabilityDetailsWithDeadline? source,
            PriceProcessFunction processFunction)
        {
            if (source == null)
                return null;

            var value = source.Value;
            var roomContractSet = await ProcessRoomContractSetPrice(value.RoomContractSet, processFunction);
            return new SingleAccommodationAvailabilityDetailsWithDeadline(value.AvailabilityId,
                value.CheckInDate,
                value.CheckOutDate,
                value.NumberOfNights,
                value.AccommodationDetails,
                roomContractSet,
                value.DeadlineDetails);
        }


        private static async Task<List<RoomContractSet>> ProcessRoomContractSetsPrices(List<RoomContractSet> sourceRoomContractSets, PriceProcessFunction priceProcessFunction)
        {
            var roomContractSets = new List<RoomContractSet>(sourceRoomContractSets.Count);
            foreach (var roomContractSet in sourceRoomContractSets)
            {
                var roomContractSetWithMarkup = await ProcessRoomContractSetPrice(roomContractSet, priceProcessFunction);
                roomContractSets.Add(roomContractSetWithMarkup);
            }

            return roomContractSets;
        }


        private static async Task<RoomContractSet> ProcessRoomContractSetPrice(RoomContractSet sourceRoomContractSet, PriceProcessFunction priceProcessFunction)
        {
            var currency = sourceRoomContractSet.Price.Currency;

            var roomContracts = new List<RoomContract>(sourceRoomContractSet.RoomContracts.Count);
            foreach (var room in sourceRoomContractSet.RoomContracts)
            {
                var roomPrices = new List<DailyPrice>(room.RoomPrices.Count);
                foreach (var roomPrice in room.RoomPrices)
                {
                    var (roomGross, roomCurrency) = await priceProcessFunction(roomPrice.Gross, currency);
                    var (roomNetTotal, _) = await priceProcessFunction(roomPrice.NetTotal, currency);

                    roomPrices.Add(BuildDailyPrice(roomPrice, roomNetTotal, roomGross, roomCurrency));
                }

                roomContracts.Add(BuildRoomContracts(room, roomPrices));
            }

            var (roomContractSetGross, roomContractSetCurrency) = await priceProcessFunction(sourceRoomContractSet.Price.Gross, currency);
            var (roomContractSetNetTotal, _) = await priceProcessFunction(sourceRoomContractSet.Price.NetTotal, currency);
            var roomContractSetPrice = new Price(roomContractSetCurrency, roomContractSetNetTotal, roomContractSetGross, sourceRoomContractSet.Price.Type,
                sourceRoomContractSet.Price.Description);

            return BuildRoomContractSet(sourceRoomContractSet, roomContractSetPrice, roomContracts);


            static DailyPrice BuildDailyPrice(in DailyPrice roomPrice, decimal roomNetTotal, decimal roomGross, Currencies roomCurrency)
                => new DailyPrice(roomPrice.FromDate, roomPrice.ToDate, roomCurrency, roomNetTotal, roomGross, roomPrice.Type, roomPrice.Description);


            static RoomContract BuildRoomContracts(in RoomContract room, List<DailyPrice> roomPrices)
                => new RoomContract(room.TariffCode, 
                    room.BoardBasisCode, 
                    room.BoardBasis, 
                    room.MealPlanCode, 
                    room.MealPlan, 
                    room.DeadlineDate,
                    room.ContractTypeId,
                    room.IsAvailableImmediately,
                    room.IsDynamic,
                    room.IsSpecial,
                    room.ContractType,
                    room.Remarks,
                    roomPrices, 
                    room.AdultsNumber, 
                    room.ChildrenNumber, 
                    room.ChildrenAges,
                    room.Type,
                    room.IsExtraBedNeeded);

            static RoomContractSet BuildRoomContractSet(in RoomContractSet roomContractSet, in Price roomContractSetPrice, List<RoomContract> rooms)
                => new RoomContractSet(roomContractSet.Id, roomContractSetPrice, rooms);
        }


        public static Currencies? GetCurrency(this CombinedAvailabilityDetails availabilityDetails)
        {
            var roomContractSets = availabilityDetails.Results
                .SelectMany(r => r.Data.Agreements)
                .ToList();

            if (!roomContractSets.Any())
                return null;
            
            return roomContractSets
                .Select(a => a.Price.Currency)
                .First();
        }
        
        public static Currencies? GetCurrency(this SingleAccommodationAvailabilityDetails availabilityDetails)
        {
            if (!availabilityDetails.Agreements.Any())
                return null;
            
            return availabilityDetails.Agreements
                .Select(a => a.Price.Currency)
                .First();
        }
        
        public static Currencies? GetCurrency(this SingleAccommodationAvailabilityDetailsWithDeadline? availabilityDetails)
        {
            return availabilityDetails?.RoomContractSet.Price.Currency;
        }
    }
}