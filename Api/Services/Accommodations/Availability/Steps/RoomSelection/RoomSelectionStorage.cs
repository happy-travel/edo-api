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
            var keyPrefix = BuildKeyPrefix(searchId, resultId);
            return _storage.Save(keyPrefix, details, dataProvider);
        }
        
        public async Task<List<(DataProviders DataProvider, SingleAccommodationAvailabilityDetails Result)>> GetResult(Guid searchId, Guid resultId, List<DataProviders> dataProviders)
        {
            var keyPrefix = BuildKeyPrefix(searchId, resultId);
            return (await _storage.Get<SingleAccommodationAvailabilityDetails>(keyPrefix, dataProviders))
                .Where(t => !string.IsNullOrWhiteSpace(t.Result.AvailabilityId))
                .ToList();
        }


        private string BuildKeyPrefix(Guid searchId, Guid resultId) => $"{searchId}::{resultId}";
        
        private readonly IMultiProviderAvailabilityStorage _storage;
    }
}