using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Money.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilityPriceProcessor : IWideAvailabilityPriceProcessor
    {
        public WideAvailabilityPriceProcessor(IPriceProcessor priceProcessor)
        {
            _priceProcessor = priceProcessor;
        }
        
        
        public async Task<EdoContracts.Accommodations.Availability> ApplyMarkups(EdoContracts.Accommodations.Availability response, AgentContext agent)
        {
            var convertedResults = new List<SlimAccommodationAvailability>(response.Results.Count);
            foreach (var slimAccommodationAvailability in response.Results)
            {
                // Currency can differ in different results
                var convertedAccommodationAvailability = await _priceProcessor.ApplyMarkups(agent,
                    slimAccommodationAvailability,
                    ProcessPrices);

                convertedResults.Add(convertedAccommodationAvailability);
            }

            return new EdoContracts.Accommodations.Availability(response.AvailabilityId, response.NumberOfNights,
                response.CheckInDate, response.CheckOutDate, convertedResults, response.NumberOfProcessedAccommodations);
        }


        public async Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> ConvertCurrencies(EdoContracts.Accommodations.Availability availabilityDetails, AgentContext agent)
        {
            var convertedResults = new List<SlimAccommodationAvailability>(availabilityDetails.Results.Count);
            foreach (var slimAccommodationAvailability in availabilityDetails.Results)
            {
                // Currency can differ in different results
                var (_, isFailure, convertedAccommodationAvailability, error) = await _priceProcessor.ConvertCurrencies(agent,
                    slimAccommodationAvailability,
                    ProcessPrices,
                    GetCurrency);

                if (isFailure)
                    return Result.Failure<EdoContracts.Accommodations.Availability, ProblemDetails>(error);
                    
                convertedResults.Add(convertedAccommodationAvailability);
            }

            return new EdoContracts.Accommodations.Availability(availabilityDetails.AvailabilityId, availabilityDetails.NumberOfNights,
                availabilityDetails.CheckInDate, availabilityDetails.CheckOutDate, convertedResults, availabilityDetails.NumberOfProcessedAccommodations);
        }
        
        
        public async ValueTask<EdoContracts.Accommodations.Availability> AlignPrices(EdoContracts.Accommodations.Availability response)
        {
            var convertedResults = new List<SlimAccommodationAvailability>(response.Results.Count);
            foreach (var slimAccommodationAvailability in response.Results)
            {
                // Currency can differ in different results
                var roomContractSets = await RoomContractSetPriceProcessor.AlignPrices(slimAccommodationAvailability.RoomContractSets);
                convertedResults.Add(new SlimAccommodationAvailability(slimAccommodationAvailability.AccommodationId,
                    roomContractSets, slimAccommodationAvailability.AvailabilityId));
            }

            return new EdoContracts.Accommodations.Availability(response.AvailabilityId, response.NumberOfNights,
                response.CheckInDate, response.CheckOutDate, convertedResults, response.NumberOfProcessedAccommodations);
        }
        
        
        private static Currencies? GetCurrency(SlimAccommodationAvailability accommodationAvailability)
        {
            if (!accommodationAvailability.RoomContractSets.Any())
                return null;

            return accommodationAvailability.RoomContractSets.First().Rate.Currency;
        }
        

        private static async ValueTask<SlimAccommodationAvailability> ProcessPrices(SlimAccommodationAvailability supplierResponse, PriceProcessFunction function)
        {
            var supplierRoomContractSets = supplierResponse.RoomContractSets;
            var roomContractSetsWithMarkup = await RoomContractSetPriceProcessor.ProcessPrices(supplierRoomContractSets, function);
            var convertedAccommodationAvailability = new SlimAccommodationAvailability(supplierResponse.AccommodationId,
                roomContractSetsWithMarkup,
                supplierResponse.AvailabilityId);
            
            return convertedAccommodationAvailability;
        }
        

        private readonly IPriceProcessor _priceProcessor;
    }
}