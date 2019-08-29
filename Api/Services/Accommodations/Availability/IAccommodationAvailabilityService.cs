using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Availabilities;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public interface IAccommodationAvailabilityService
    {
        ValueTask<Result<CombinedAvailabilityResponse, ProblemDetails>> GetAvailable(AvailabilityRequest request, string languageCode);
    }
}