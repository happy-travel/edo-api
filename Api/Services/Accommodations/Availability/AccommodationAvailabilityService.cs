using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Services.DataProviders;
using HappyTravel.Edo.Api.Services.Locations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AccommodationAvailabilityService : IAccommodationAvailabilityService
    {
        private readonly ILocationService _locationService;
        private readonly IDataProviderCollection _dataProviders;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;

        public AccommodationAvailabilityService(ILocationService locationService, 
            IDataProviderCollection dataProviders,
            IAvailabilityResultsCache availabilityResultsCache)
        {
            _locationService = locationService;
            _dataProviders = dataProviders;
            _availabilityResultsCache = availabilityResultsCache;
        }
        
        public async ValueTask<Result<CombinedAvailabilityResponse, ProblemDetails>> GetAvailable(AvailabilityRequest request, string languageCode)
        {
            var (_, isFailure, location, error) = await _locationService.Get(request.Location, languageCode);
            if (isFailure)
                return Result.Fail<CombinedAvailabilityResponse, ProblemDetails>(error);

            var allResults = _dataProviders.Get()
                .Select(async provider => await provider.Accommodations.GetAvailable(request, location))
                .ToArray();
            
            var combinedResult = Result.Combine(allResults);
            if(combinedResult.IsFailure)
                return ProblemDetailsBuilder.Fail<CombinedAvailabilityResponse>(combinedResult.Error);

            return Result.Ok<CombinedAvailabilityResponse, ProblemDetails>(new CombinedAvailabilityResponse(allResults.Select(res=> res.Value).ToList()));
        }
    }
}