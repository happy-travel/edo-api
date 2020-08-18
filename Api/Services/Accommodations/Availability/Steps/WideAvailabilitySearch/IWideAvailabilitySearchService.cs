using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public interface IWideAvailabilitySearchService
    {
        Task<Result<Guid>> StartSearch(AvailabilityRequest request, AgentContext agent, string languageCode);

        Task<WideAvailabilitySearchState> GetState(Guid searchId);

        Task<IEnumerable<AvailabilityResult>> GetResult(Guid searchId, AgentContext agent);

        Task<Result<ProviderData<DeadlineDetails>, ProblemDetails>> GetDeadlineDetails(
            DataProviders dataProvider, string availabilityId, Guid roomContractSetId, string languageCode);
    }
}