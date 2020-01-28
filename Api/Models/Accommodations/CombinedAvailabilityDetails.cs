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

        /// <summary>
        /// Number of nights
        /// </summary>
        public int NumberOfNights { get; }
        
        /// <summary>
        /// Check-in date
        /// </summary>
        public DateTime CheckInDate { get; }
        
        /// <summary>
        /// Check-out date
        /// </summary>
        public DateTime CheckOutDate { get; }
        
        /// <summary>
        /// Availability results, grouped by accommodation
        /// </summary>
        public List<ProviderData<AvailabilityResult>> Results { get; }
    }
}