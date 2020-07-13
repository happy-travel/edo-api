using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public interface IAvailabilityStorage
    {
        Task SaveResult(Guid searchId, DataProviders dataProvider, AvailabilityDetails details);

        Task SetState(Guid searchId, DataProviders dataProvider, AvailabilitySearchState searchState);

        Task<IEnumerable<ProviderData<AvailabilityResult>>> GetResult(Guid searchId);

        Task<AvailabilitySearchState> GetState(Guid searchId);
    }
}