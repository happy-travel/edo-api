using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public interface IRoomSelectionPriceProcessor
    {
        Task<Result<AccommodationAvailability, ProblemDetails>> ConvertCurrencies(AccommodationAvailability availabilityDetails, AgentContext agent);

        Task<AccommodationAvailability> ApplyMarkups(AccommodationAvailability response, AgentContext agent);
        
        ValueTask<AccommodationAvailability> AlignPrices(AccommodationAvailability availabilityDetails);
    }
}