using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public interface IRoomSelectionStorage
    {
        Task SaveResult(Guid searchId, Guid resultId, SingleAccommodationAvailabilityDetails details, DataProviders dataProvider);

        Task<(DataProviders DataProvider, SingleAccommodationAvailabilityDetails Result)[]> GetResult(Guid searchId, Guid resultId, List<DataProviders> dataProviders);
    }
}