using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public interface IAvailabilityStorage
    {
        Task SaveResult(Guid searchId, DataProviders dataProvider, AvailabilityDetails details);
        
        Task SaveRequest(Guid searchId, HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest request);
        
        Task<Result<HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest>> GetRequest(Guid searchId);

        Task SetState(Guid searchId, DataProviders dataProvider, AvailabilitySearchState searchState);

        Task<IEnumerable<ProviderData<AvailabilityResult>>> GetResult(Guid searchId, AgentContext agent);

        Task<AvailabilitySearchState> GetState(Guid searchId);
    }
}