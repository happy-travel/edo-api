using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public interface IWideAvailabilityPriceProcessor
    {
        Task<EdoContracts.Accommodations.Availability> ApplyMarkups(EdoContracts.Accommodations.Availability response, AgentContext agent);

        Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> ConvertCurrencies(EdoContracts.Accommodations.Availability availabilityDetails, AgentContext agent);

        ValueTask<EdoContracts.Accommodations.Availability> AlignPrices(EdoContracts.Accommodations.Availability response);
    }
}