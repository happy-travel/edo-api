using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionStorage : IRoomSelectionStorage
    {
        public RoomSelectionStorage(IMultiProviderAvailabilityStorage storage)
        {
            _storage = storage;
        }


        public Task SaveResult(Guid searchId, Guid resultId, SingleAccommodationAvailabilityDetails details, DataProviders dataProvider)
        {
            var keyPrefix = CreateKeyPrefix(searchId, resultId);
            return _storage.SaveObject(keyPrefix, details, dataProvider);
        }
        
        public async Task<(DataProviders DataProvider, SingleAccommodationAvailabilityDetails Result)[]> GetResult(Guid searchId, Guid resultId, List<DataProviders> dataProviders)
        {
            var keyPrefix = CreateKeyPrefix(searchId, resultId);
            return (await _storage.GetProviderResults<SingleAccommodationAvailabilityDetails>(keyPrefix, dataProviders))
                .Where(t => !string.IsNullOrWhiteSpace(t.Result.AvailabilityId))
                .ToArray();
        }


        private string CreateKeyPrefix(Guid searchId, Guid resultId) => $"{searchId}::{resultId}";
        
        private readonly IMultiProviderAvailabilityStorage _storage;
    }
}