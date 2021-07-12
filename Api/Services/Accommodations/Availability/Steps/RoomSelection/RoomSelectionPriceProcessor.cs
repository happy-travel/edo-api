using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Money.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionPriceProcessor : IRoomSelectionPriceProcessor
    {
        public RoomSelectionPriceProcessor(IPriceProcessor priceProcessor)
        {
            _priceProcessor = priceProcessor;
        }
        
        
        public Task<Result<AccommodationAvailability, ProblemDetails>> ConvertCurrencies(AccommodationAvailability availabilityDetails, AgentContext agent)
            => _priceProcessor.ConvertCurrencies(agent, availabilityDetails, ProcessPrices, GetCurrency);


        public Task<AccommodationAvailability> ApplyMarkups(AccommodationAvailability response, AgentContext agent)
            => _priceProcessor.ApplyMarkups(agent, response, ProcessPrices);


        public async ValueTask<AccommodationAvailability> AlignPrices(AccommodationAvailability source)
        {
            var roomContractSets = await RoomContractSetPriceProcessor.AlignPrices(source.RoomContractSets);
            return new AccommodationAvailability(availabilityId: source.AvailabilityId,
                accommodationId: source.AccommodationId,
                checkInDate: source.CheckInDate,
                checkOutDate: source.CheckOutDate,
                numberOfNights: source.NumberOfNights,
                roomContractSets: roomContractSets);
        }


        private static async ValueTask<AccommodationAvailability> ProcessPrices(AccommodationAvailability source,
            PriceProcessFunction processFunction)
        {
            var roomContractSets = await RoomContractSetPriceProcessor.ProcessPrices(source.RoomContractSets, processFunction);
            return new AccommodationAvailability(availabilityId: source.AvailabilityId,
                accommodationId: source.AccommodationId,
                checkInDate: source.CheckInDate,
                checkOutDate: source.CheckOutDate,
                numberOfNights: source.NumberOfNights,
                roomContractSets: roomContractSets);
        }


        private static Currencies? GetCurrency(AccommodationAvailability availabilityDetails)
        {
            if (!availabilityDetails.RoomContractSets.Any())
                return null;
            
            return availabilityDetails.RoomContractSets
                .Select(a => a.Rate.Currency)
                .First();
        }
        
        
        private readonly IPriceProcessor _priceProcessor;
    }
}