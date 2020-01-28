using System.Collections.Generic;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Data.Markup;

namespace HappyTravel.Edo.Api.Models.Markups.Availability
{
    public readonly struct AvailabilityDetailsWithMarkup
    {
        public AvailabilityDetailsWithMarkup(List<MarkupPolicy> appliedPolicies, CombinedAvailabilityDetails resultResponse)
        {
            AppliedPolicies = appliedPolicies;
            ResultResponse = resultResponse;
        }


        public List<MarkupPolicy> AppliedPolicies { get; }
        public CombinedAvailabilityDetails ResultResponse { get; }
    }
}