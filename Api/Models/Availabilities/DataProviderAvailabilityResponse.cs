using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Availabilities
{
    public readonly struct DataProviderAvailabilityResponse
    {
        [JsonConstructor]
        private DataProviderAvailabilityResponse(int availabilityId, int numberOfNights, DateTime checkInDate, DateTime checkOutDate, List<SlimAvailabilityResult> results)
        {
            AvailabilityId = availabilityId;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            NumberOfNights = numberOfNights;
            Results = results;
        }
        
        public int AvailabilityId { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public int NumberOfNights { get; }
        public List<SlimAvailabilityResult> Results { get; }
        
        public override bool Equals(object obj) => obj is DataProviderAvailabilityResponse other && Equals(other);

        public bool Equals(DataProviderAvailabilityResponse other)
        {
            return (AvailabilityId, CheckInDate, NumberOfNights, Results) ==
                   (other.AvailabilityId, other.CheckInDate, other.NumberOfNights, other.Results);
        }

        public override int GetHashCode() => (AvailabilityId, CheckInDate, NumberOfNights, Results).GetHashCode();
    }
}
