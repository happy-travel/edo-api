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
        Task<Result<ProviderData<SingleAccommodationAvailabilityDetails>, ProblemDetails>> GetAvailable(DataProviders dataProvider, Guid searchId, string accommodationId, string languageCode);
        
        Task<Result<ProviderData<SingleAccommodationAvailabilityDetailsWithDeadline?>, ProblemDetails>> GetExactAvailability(DataProviders dataProvider, string availabilityId, Guid roomContractSetId,
            string languageCode);

        Task<Result<ProviderData<DeadlineDetails>, ProblemDetails>> GetDeadlineDetails(DataProviders dataProvider, string availabilityId, Guid roomContractSetId, string languageCode);
    }
}