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
        Task<Result<CombinedAvailabilityDetails, ProblemDetails>> GetAvailable(Models.Availabilities.AvailabilityRequest request, string languageCode);

        Task<Result<ProviderData<SingleAccommodationAvailabilityDetails>, ProblemDetails>> GetAvailable(DataProviders dataProvider, string accommodationId, string availabilityId, string languageCode);
        
        Task<Result<ProviderData<SingleAccommodationAvailabilityDetailsWithDeadline?>, ProblemDetails>> GetExactAvailability(DataProviders dataProvider, string availabilityId, Guid roomContractSetId,
            string languageCode);

        Task<Result<ProviderData<DeadlineDetails>, ProblemDetails>> GetDeadlineDetails(DataProviders dataProvider, string availabilityId, Guid roomContractSetId, string languageCode);
    }
}