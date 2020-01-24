using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public readonly struct AvailabilityResult
    {
        public AvailabilityResult(long availabilityId, SlimAccommodationDetails accommodationDetails, List<Agreement> agreements)
        {
            AvailabilityId = availabilityId;
            AccommodationDetails = accommodationDetails;
            Agreements = agreements;
        }
        
        public long AvailabilityId { get; }
        public SlimAccommodationDetails AccommodationDetails { get; }
        public List<Agreement> Agreements { get; }
    }
}