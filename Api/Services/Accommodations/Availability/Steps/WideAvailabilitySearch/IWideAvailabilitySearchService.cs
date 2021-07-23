using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public interface IWideAvailabilitySearchService
    {
        Task<Result<Guid>> StartSearch(AvailabilityRequest request, AgentContext agent, string languageCode);

        Task<WideAvailabilitySearchState> GetState(Guid searchId, AgentContext agent);

        Task<IEnumerable<WideAvailabilityResult>> GetResult(Guid searchId, int top, int skip, AgentContext agent, string languageCode);
    }
}