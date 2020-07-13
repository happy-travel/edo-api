using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public static class AvailabilityResultsExtensions
    {
        public static async ValueTask<SingleAccommodationAvailabilityDetails> ProcessPrices(this SingleAccommodationAvailabilityDetails source,
            PriceProcessFunction processFunction)
        {
            var roomContractSets = await ProcessRoomContractSetsPrices(source.RoomContractSets, processFunction);
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
                roomContractSet);
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

                var totalPriceGross = await priceProcessFunction(room.TotalPrice.NetTotal, currency);
                var totalPriceNet = await priceProcessFunction(room.TotalPrice.Gross, currency);
                var totalPrice = new Price(totalPriceGross.Currency, totalPriceNet.Amount, totalPriceGross.Amount);

                roomContracts.Add(BuildRoomContracts(room, roomPrices, totalPrice));
            }

            var (roomContractSetGross, roomContractSetCurrency) = await priceProcessFunction(sourceRoomContractSet.Price.Gross, currency);
            var (roomContractSetNetTotal, _) = await priceProcessFunction(sourceRoomContractSet.Price.NetTotal, currency);
            var roomContractSetPrice = new Price(roomContractSetCurrency, roomContractSetNetTotal, roomContractSetGross, sourceRoomContractSet.Price.Type,
                sourceRoomContractSet.Price.Description);

            return BuildRoomContractSet(sourceRoomContractSet, roomContractSetPrice, roomContracts);


            static DailyPrice BuildDailyPrice(in DailyPrice roomPrice, decimal roomNetTotal, decimal roomGross, Currencies roomCurrency)
                => new DailyPrice(roomPrice.FromDate, roomPrice.ToDate, roomCurrency, roomNetTotal, roomGross, roomPrice.Type, roomPrice.Description);


            static RoomContract BuildRoomContracts(in RoomContract room, List<DailyPrice> roomPrices, Price totalPrice)
                => new RoomContract(room.BoardBasis, 
                    room.MealPlan, 
                    room.DeadlineDate,
                    room.ContractType,
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
                    room.DeadlineDetails);

            static RoomContractSet BuildRoomContractSet(in RoomContractSet roomContractSet, in Price roomContractSetPrice, List<RoomContract> rooms)
                => new RoomContractSet(roomContractSet.Id, roomContractSetPrice,roomContractSet.DeadlineDate, rooms);
        }


        public static Currencies? GetCurrency(this SingleAccommodationAvailabilityDetails availabilityDetails)
        {
            if (!availabilityDetails.RoomContractSets.Any())
                return null;
            
            return availabilityDetails.RoomContractSets
                .Select(a => a.Price.Currency)
                .First();
        }
        
        public static Currencies? GetCurrency(this SingleAccommodationAvailabilityDetailsWithDeadline? availabilityDetails)
        {
            return availabilityDetails?.RoomContractSet.Price.Currency;
        }


        public static async ValueTask<AvailabilityDetails> ProcessPrices(AvailabilityDetails details, PriceProcessFunction processFunction)
        {
            var accommodationAvailabilities = new List<AccommodationAvailabilityDetails>(details.Results.Count);
            foreach (var supplierResponse in details.Results)
            {
                var supplierRoomContractSets = supplierResponse.RoomContractSets;
                var roomContractSetsWithMarkup = await ProcessRoomContractSetsPrices(supplierRoomContractSets, processFunction);
                accommodationAvailabilities.Add(new AccommodationAvailabilityDetails(supplierResponse.AccommodationDetails, roomContractSetsWithMarkup));
            }

            return new AvailabilityDetails(details.AvailabilityId, details.NumberOfNights, details.CheckInDate, details.CheckOutDate, accommodationAvailabilities, details.NumberOfProcessedAccommodations);
        }


        public static Currencies? GetCurrency(AvailabilityDetails details)
        {
            if (!details.Results.Any())
                return null;
            
            return details.Results.First().RoomContractSets.First().Price.Currency;
        }
    }
}