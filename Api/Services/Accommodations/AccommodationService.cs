using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Locations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(IMemoryFlow flow,
            IOptions<DataProviderOptions> options,
            IDataProviderClient dataProviderClient,
            ILocationService locationService,
            IAccommodationBookingManager accommodationBookingManager,
            IAvailabilityResultsCache availabilityResultsCache)
        {
            _flow = flow;
            _dataProviderClient = dataProviderClient;
            _locationService = locationService;
            _accommodationBookingManager = accommodationBookingManager;
            _availabilityResultsCache = availabilityResultsCache;

            _options = options.Value;
        }


        public ValueTask<Result<RichAccommodationDetails, ProblemDetails>> Get(string accommodationId, string languageCode)
            => _flow.GetOrSetAsync(_flow.BuildKey(nameof(AccommodationService), "Accommodations", languageCode, accommodationId),
                async () => await _dataProviderClient.Get<RichAccommodationDetails>(new Uri($"{_options.Netstorming}hotels/{accommodationId}", UriKind.Absolute),
                    languageCode),
                TimeSpan.FromDays(1));


        public async ValueTask<Result<AvailabilityResponse, ProblemDetails>> GetAvailable(AvailabilityRequest request, string languageCode)
        {
            var (_, isFailure, location, error) = await _locationService.Get(request.Location, languageCode);
            if (isFailure)
                return Result.Fail<AvailabilityResponse, ProblemDetails>(error);

            return await _dataProviderClient.Post<InnerAvailabilityRequest, AvailabilityResponse>(new Uri(_options.Netstorming + "hotels/availability", UriKind.Absolute),
                new InnerAvailabilityRequest(request, location), languageCode)
                .OnSuccess(response => _availabilityResultsCache.Set(response));
        }


        public async Task<Result<AccommodationBookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request, string languageCode)
        {
            var availability = await GetSelectedAvailability(request.AvailabilityId, request.AgreementId);
            if(availability.Equals(default))
                return ProblemDetailsBuilder.Fail<AccommodationBookingDetails>("Could not find availability by given id");
            
            return await _accommodationBookingManager.Book(request, availability, languageCode);
            
            async ValueTask<BookingAvailabilityInfo> GetSelectedAvailability(int availabilityId, Guid agreementId)
            {
                var availabilityResponse = await _availabilityResultsCache.Get(availabilityId);
                if (availabilityResponse.Equals(default))
                    return default;
                    
                return (from availabilityResult in availabilityResponse.Results
                        from agreement in availabilityResult.Agreements
                        where agreement.Id == agreementId
                        select new BookingAvailabilityInfo(availabilityResult.AccommodationDetails.Id, 
                            agreement,
                            availabilityResult.AccommodationDetails.Location.CountryCode,
                            availabilityResponse.CheckInDate,
                            availabilityResponse.CheckOutDate))
                    .SingleOrDefault();
            }
        }

<<<<<<< Updated upstream
=======
        public Task<AccommodationBookingInfo[]> GetBookings()
        {
            return _accommodationBookingManager.GetBookings();
        }

        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(int bookingId)
        {
            return _accommodationBookingManager.Cancel(bookingId);
        }

>>>>>>> Stashed changes
        private readonly IDataProviderClient _dataProviderClient;
        private readonly IMemoryFlow _flow;
        private readonly ILocationService _locationService;
        private readonly IAccommodationBookingManager _accommodationBookingManager;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly DataProviderOptions _options;
    }
}