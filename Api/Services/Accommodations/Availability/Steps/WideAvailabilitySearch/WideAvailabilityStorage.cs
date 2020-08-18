using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilityStorage : IWideAvailabilityStorage
    {
        public WideAvailabilityStorage(IMultiProviderAvailabilityStorage multiProviderAvailabilityStorage)
        {
            _multiProviderAvailabilityStorage = multiProviderAvailabilityStorage;
        }


        public async Task<(DataProviders ProviderKey, AccommodationAvailabilityResult[] AccommodationAvailabilities)[]> GetResults(Guid searchId, List<DataProviders> dataProviders)
        {
            return  (await _multiProviderAvailabilityStorage.GetProviderResults<AccommodationAvailabilityResult[]>(searchId.ToString(), dataProviders, true))
                .Where(t => t.Result != default)
                .ToArray();
        }


        public Task<(DataProviders ProviderKey, ProviderAvailabilitySearchState States)[]> GetStates(Guid searchId,
            List<DataProviders> dataProviders)
        {
            return _multiProviderAvailabilityStorage.GetProviderResults<ProviderAvailabilitySearchState>(searchId.ToString(), dataProviders, false);
        }


        public Task SaveState(Guid searchId, ProviderAvailabilitySearchState state, DataProviders dataProviders)
        {
            return _multiProviderAvailabilityStorage.SaveObject(searchId.ToString(), state, dataProviders);
        }


        public Task SaveResults(Guid searchId, DataProviders dataProvider, AccommodationAvailabilityResult[] results)
        {
            return _multiProviderAvailabilityStorage.SaveObject(searchId.ToString(), results, dataProvider);
        }
        
        private readonly IMultiProviderAvailabilityStorage _multiProviderAvailabilityStorage;
    }
}