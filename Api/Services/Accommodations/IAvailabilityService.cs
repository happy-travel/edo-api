using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IAvailabilityService
    {
        ValueTask<Result<CombinedAvailabilityDetails, ProblemDetails>> GetAvailable(Edo.Api.Models.Availabilities.AvailabilityRequest request, string languageCode);


        Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(DataProviders source, string accommodationId, long availabilityId, 
            string languageCode);


        Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(DataProviders source, long availabilityId, Guid agreementId,
            string languageCode);
    }
}