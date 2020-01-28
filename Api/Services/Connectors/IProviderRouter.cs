using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IProviderRouter
    {
        Task<Result<CombinedAvailabilityDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode);
        
        Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(DataProviders dataProvider, string accommodationId, long availabilityId, string languageCode);
        
        Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(DataProviders dataProvider, long availabilityId, Guid agreementId,
            string languageCode);
    }
}