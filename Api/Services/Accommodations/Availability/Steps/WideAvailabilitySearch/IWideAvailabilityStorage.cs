using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public interface IWideAvailabilityStorage
    {
        Task<List<(Suppliers ProviderKey, List<AccommodationAvailabilityResult> AccommodationAvailabilities)>> GetResults(Guid searchId, List<Suppliers> dataProviders);

        Task SaveResults(Guid searchId, Suppliers supplier, List<AccommodationAvailabilityResult> results);

        Task<List<(Suppliers ProviderKey, ProviderAvailabilitySearchState States)>> GetStates(Guid searchId,
            List<Suppliers> dataProviders);
        
        Task SaveState(Guid searchId, ProviderAvailabilitySearchState state, Suppliers supplier);
    }
}