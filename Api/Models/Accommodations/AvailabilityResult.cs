using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct AvailabilityResult
    {
        public AvailabilityResult(long availabilityId, SlimAccommodationDetails accommodationDetails, List<Agreement> agreements)
        {
            AvailabilityId = availabilityId;
            AccommodationDetails = accommodationDetails;
            Agreements = agreements ?? new List<Agreement>(0);
        }
        
        public long AvailabilityId { get; }
        public SlimAccommodationDetails AccommodationDetails { get; }
        public List<Agreement> Agreements { get; }
    }
}