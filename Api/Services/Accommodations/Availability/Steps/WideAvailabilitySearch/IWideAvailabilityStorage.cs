using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public interface IWideAvailabilityStorage
    {
        Task<(DataProviders ProviderKey, AccommodationAvailabilityResult[] AccommodationAvailabilities)[]> GetResults(Guid searchId, List<DataProviders> dataProviders);

        Task SaveResults(Guid searchId, DataProviders dataProvider, AccommodationAvailabilityResult[] results);

        Task<(DataProviders ProviderKey, ProviderAvailabilitySearchState States)[]> GetStates(Guid searchId,
            List<DataProviders> dataProviders);
        
        Task SaveState(Guid searchId, ProviderAvailabilitySearchState state, DataProviders dataProviders);
    }
}