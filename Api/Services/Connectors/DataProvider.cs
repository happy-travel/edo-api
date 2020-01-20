using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class DataProvider : IDataProvider
    {
        private readonly IDataProviderClient _dataProviderClient;
        private readonly string _baseUrl;

        public DataProvider(IDataProviderClient dataProviderClient, string baseUrl)
        {
            _dataProviderClient = dataProviderClient;
            _baseUrl = baseUrl;
        }


        public Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode)
        {
            return _dataProviderClient.Post<AvailabilityRequest, AvailabilityDetails>(
                new Uri(_baseUrl + "availabilities/accommodations", UriKind.Absolute), availabilityRequest, languageCode);
        }


        public Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailability(long availabilityId,
            string accommodationId, string languageCode)
        {
            return _dataProviderClient.Post<SingleAccommodationAvailabilityDetails>(
                new Uri(_baseUrl + "accommodations/" + accommodationId + "/availabilities/" + availabilityId, UriKind.Absolute), languageCode);
        }


        public Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(string accommodationId, string availabilityId, string agreementCode, string languageCode)
        {
            var uri = new Uri($"{_baseUrl}accommodations/{accommodationId}/deadline/{availabilityId}/{agreementCode}", UriKind.Absolute);
            return _dataProviderClient.Get<DeadlineDetails>(uri, languageCode);
        }


        public Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(string accommodationId, string languageCode)
        {
            return _dataProviderClient.Get<AccommodationDetails>(
                new Uri($"{_baseUrl}accommodations/{accommodationId}", UriKind.Absolute), languageCode);
        }


        public Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(long availabilityId, string agreementId, string languageCode)
        {
            return _dataProviderClient.Post<SingleAccommodationAvailabilityDetailsWithDeadline>(
                new Uri($"{_baseUrl}accommodations/availabilities/{availabilityId}/agreements/{agreementId}", UriKind.Absolute), languageCode);
        }


        public Task<Result<BookingDetails, ProblemDetails>> Book(BookingRequest request, string languageCode)
        {
            return _dataProviderClient.Post<BookingRequest, BookingDetails>(
                new Uri(_baseUrl + "bookings/accommodations", UriKind.Absolute),
                request, languageCode);
        }


        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(string referenceCode)
        {
            return _dataProviderClient.Post(new Uri(_baseUrl + "bookings/accommodations/" + referenceCode + "/cancel",
                UriKind.Absolute));
        }
    }
}