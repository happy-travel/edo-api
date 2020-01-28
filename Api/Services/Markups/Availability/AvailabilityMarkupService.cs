using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Markups.Availability;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Markups.Availability
{
    public class AvailabilityMarkupService : IAvailabilityMarkupService
    {
        public AvailabilityMarkupService(IMarkupService markupService)
        {
            _markupService = markupService;
        }


        public async Task<AvailabilityDetailsWithMarkup> Apply(CustomerInfo customerInfo, CombinedAvailabilityDetails supplierResponse)
        {
            var markup = await _markupService.Get(customerInfo, AvailabilityPolicyTarget);
            var resultResponse = await ApplyMarkup(supplierResponse, markup.Function);
            return new AvailabilityDetailsWithMarkup(markup.Policies, resultResponse);
        }


        public async Task<SingleAccommodationAvailabilityDetailsWithMarkup> Apply(CustomerInfo customerInfo,
            SingleAccommodationAvailabilityDetails supplierResponse)
        {
            var markup = await _markupService.Get(customerInfo, AvailabilityPolicyTarget);
            var resultResponse = await ApplyMarkup(supplierResponse, markup.Function);
            return new SingleAccommodationAvailabilityDetailsWithMarkup(markup.Policies, resultResponse);
        }


        private static async ValueTask<SingleAccommodationAvailabilityDetails> ApplyMarkup(SingleAccommodationAvailabilityDetails supplierResponse,
            AggregatedMarkupFunction aggregatedMarkupFunction)
        {
            var agreements = await ApplyMarkupToAgreements(supplierResponse.Agreements, aggregatedMarkupFunction);
            return new SingleAccommodationAvailabilityDetails(supplierResponse.AvailabilityId,
                supplierResponse.CheckInDate,
                supplierResponse.CheckOutDate,
                supplierResponse.NumberOfNights,
                supplierResponse.AccommodationDetails,
                agreements);
        }


        private static async ValueTask<CombinedAvailabilityDetails> ApplyMarkup(CombinedAvailabilityDetails supplierResponse, AggregatedMarkupFunction aggregatedMarkupFunction)
        {
            // TODO: Add markup application
            return supplierResponse;

            // var availabilityResults = new List<SlimAvailabilityResult>(supplierResponse.Results.Count);
            // foreach (var availabilityResult in supplierResponse.Results)
            // {
            //     var agreements = await ApplyMarkupToAgreements(availabilityResult.Agreements, aggregatedMarkupFunction);
            //     availabilityResults.Add(new SlimAvailabilityResult(availabilityResult.AccommodationDetails, agreements));
            // }
            //
            // return new CombinedAvailabilityDetails(supplierResponse.NumberOfNights, supplierResponse.CheckInDate,
            //     supplierResponse.CheckOutDate, availabilityResults);
        }


        private static async Task<List<Agreement>> ApplyMarkupToAgreements(List<Agreement> sourceAgreements, AggregatedMarkupFunction aggregatedMarkupFunction)
        {
            var agreements = new List<Agreement>(sourceAgreements.Count);
            var currency = agreements.FirstOrDefault().Price.Currency;
            foreach (var agreement in sourceAgreements)
            {
                var rooms = new List<RoomDetails>(agreement.Rooms.Count);
                foreach (var room in agreement.Rooms)
                {
                    var roomPrices = new List<DailyPrice>(room.RoomPrices.Count);
                    foreach (var roomPrice in room.RoomPrices)
                    {
                        var roomGross = await aggregatedMarkupFunction(roomPrice.Gross, currency);
                        var roomNetTotal = await aggregatedMarkupFunction(roomPrice.NetTotal, currency);

                        roomPrices.Add(BuildDailyPrice(roomPrice, roomNetTotal, roomGross));
                    }

                    rooms.Add(BuildRoomDetails(room, roomPrices));
                }

                var agreementGross = await aggregatedMarkupFunction(agreement.Price.Gross, currency);
                var agreementNetTotal = await aggregatedMarkupFunction(agreement.Price.NetTotal, currency);
                var agreementPrice = new Price(agreement.Price.Currency, agreementNetTotal, agreementGross, agreement.Price.Type,
                    agreement.Price.Description);

                agreements.Add(BuildAgreement(agreement, agreementPrice, rooms));
            }

            return agreements;


            DailyPrice BuildDailyPrice(in DailyPrice roomPrice, decimal roomNetTotal, decimal roomGross)
                => new DailyPrice(roomPrice.FromDate, roomPrice.ToDate, roomPrice.Currency, roomNetTotal, roomGross, roomPrice.Type, roomPrice.Description);


            RoomDetails BuildRoomDetails(in RoomDetails room, List<DailyPrice> roomPrices)
                => new RoomDetails(roomPrices, room.AdultsNumber, room.ChildrenNumber, room.ChildrenAges, room.Type, room.IsExtraBedNeeded);


            Agreement BuildAgreement(in Agreement agreement, in Price agreementPrice, List<RoomDetails> rooms)
                => new Agreement(agreement.Id, agreement.TariffCode, agreement.BoardBasisCode, agreement.BoardBasis, agreement.MealPlanCode, agreement.MealPlan,
                    agreement.DeadlineDate,
                    agreement.ContractTypeId, agreement.IsAvailableImmediately, agreement.IsDynamic, agreement.IsSpecial, agreementPrice, rooms,
                    agreement.ContractType, agreement.Remarks);
        }


        private static Currencies GetCurrency(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return Currencies.NotSpecified;

            return Enum.TryParse<Currencies>(currencyCode, out var currency)
                ? currency
                : Currencies.NotSpecified;
        }


        private const MarkupPolicyTarget AvailabilityPolicyTarget = MarkupPolicyTarget.AccommodationAvailability;
        private readonly IMarkupService _markupService;
    }
}