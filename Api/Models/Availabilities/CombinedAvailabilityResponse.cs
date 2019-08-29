using System;
using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Availabilities
{
    public struct CombinedAvailabilityResponse
    {
        public CombinedAvailabilityResponse(List<DataProviderAvailabilityResponse> responses)
        {
            Responses = responses;
            Id = Guid.NewGuid();
;        }

        public Guid Id { get; }
        
        public List<DataProviderAvailabilityResponse> Responses { get; }
    }
}