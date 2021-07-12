using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Money.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingEvaluationPriceProcessor : IBookingEvaluationPriceProcessor
    {
        public BookingEvaluationPriceProcessor(IPriceProcessor priceProcessor)
        {
            _priceProcessor = priceProcessor;
        }
        
        
        public Task<RoomContractSetAvailability?> ApplyMarkups(RoomContractSetAvailability? response, AgentContext agent, Action<MarkupApplicationResult<RoomContractSetAvailability?>> logAction) 
            => _priceProcessor.ApplyMarkups(agent, response, ProcessPrices, logAction);

        
        public Task<Result<RoomContractSetAvailability?, ProblemDetails>> ConvertCurrencies(RoomContractSetAvailability? availabilityDetails, AgentContext agent) 
            => _priceProcessor.ConvertCurrencies(agent, availabilityDetails, ProcessPrices, GetCurrency);


        public async ValueTask<RoomContractSetAvailability?> AlignPrices(RoomContractSetAvailability? availabilityDetails)
        {
            if (availabilityDetails == null)
                return null;
            
            var value = availabilityDetails.Value;
            var roomContractSet = await RoomContractSetPriceProcessor.AlignPrices(value.RoomContractSet);
            return new RoomContractSetAvailability(availabilityId: value.AvailabilityId,
                accommodationId: value.AccommodationId,
                checkInDate: value.CheckInDate,
                checkOutDate: value.CheckOutDate,
                numberOfNights: value.NumberOfNights,
                roomContractSet: roomContractSet);
        }


        private static async ValueTask<RoomContractSetAvailability?> ProcessPrices(RoomContractSetAvailability? source,
            PriceProcessFunction processFunction)
        {
            if (source == null)
                return null;

            var value = source.Value;
            var roomContractSet = await RoomContractSetPriceProcessor.ProcessPrices(value.RoomContractSet, processFunction);
            return new RoomContractSetAvailability(availabilityId: value.AvailabilityId,
                accommodationId: value.AccommodationId,
                checkInDate: value.CheckInDate,
                checkOutDate: value.CheckOutDate,
                numberOfNights: value.NumberOfNights,
                roomContractSet: roomContractSet);
        }


        private static Currencies? GetCurrency(RoomContractSetAvailability? availabilityDetails) 
            => availabilityDetails?.RoomContractSet.Rate.Currency;

        
        private readonly IPriceProcessor _priceProcessor;
    }
}