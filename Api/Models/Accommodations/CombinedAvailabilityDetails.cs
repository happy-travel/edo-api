using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct CombinedAvailabilityDetails
    {
        [JsonConstructor]
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