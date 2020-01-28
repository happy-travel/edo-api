using System;
using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct CombinedAvailabilityDetails
    {
        public CombinedAvailabilityDetails(int numberOfNights, DateTime checkInDate, DateTime checkOutDate, List<ProviderData<AvailabilityResult>> results)
        {
            NumberOfNights = numberOfNights;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            Results = results;
        }

        public int NumberOfNights { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public List<ProviderData<AvailabilityResult>> Results { get; }
    }
}