using System;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class DataProvider : IDataProvider
    {
        public DataProvider(IDataProviderClient dataProviderClient, string baseUrl, ILogger<DataProvider> logger)
        {
            _dataProviderClient = dataProviderClient;
            _baseUrl = baseUrl;
            _logger = logger;
        }
        
        
        public Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest request, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<AvailabilityRequest, AvailabilityDetails>(
                    new Uri(_baseUrl + "accommodations/availabilities", UriKind.Absolute), request, requestMetadata);
            });
        }


        public Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailability(string availabilityId,
            string accommodationId, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<SingleAccommodationAvailabilityDetails>(
                    new Uri(_baseUrl + "accommodations/" + accommodationId + "/availabilities/" + availabilityId, UriKind.Absolute), requestMetadata);
            });
        }
        
        
        public Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> GetExactAvailability(string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<SingleAccommodationAvailabilityDetailsWithDeadline?>(
                    new Uri($"{_baseUrl}accommodations/availabilities/{availabilityId}/room-contract-sets/{roomContractSetId}", UriKind.Absolute), requestMetadata);
            });
        }


        public Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                var uri = new Uri($"{_baseUrl}accommodations/availabilities/{availabilityId}/room-contract-sets/{roomContractSetId}/deadline", UriKind.Absolute);
                return _dataProviderClient.Get<DeadlineDetails>(uri, requestMetadata);
            });
        }


        public Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(string accommodationId, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Get<AccommodationDetails>(
                    new Uri($"{_baseUrl}accommodations/{accommodationId}", UriKind.Absolute), requestMetadata);
            });
        }


        public Task<Result<BookingDetails, ProblemDetails>> Book(BookingRequest request, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<BookingRequest, BookingDetails>(
                    new Uri(_baseUrl + "accommodations/bookings", UriKind.Absolute),
                    request, requestMetadata);
            });
        }


        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(string referenceCode, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post(new Uri(_baseUrl + "accommodations/bookings/" + referenceCode + "/cancel",
                    UriKind.Absolute), requestMetadata);
            });
        }


        public Task<Result<BookingDetails, ProblemDetails>> GetBookingDetails(string referenceCode, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Get<BookingDetails>(
                    new Uri(_baseUrl + "accommodations/bookings/" + referenceCode,
                        UriKind.Absolute), requestMetadata);
            });
        }


        public Task<Result<BookingDetails, ProblemDetails>> ProcessAsyncResponse(Stream stream, RequestMetadata requestMetadata)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<BookingDetails>(new Uri(_baseUrl + "bookings/response", UriKind.Absolute), stream, requestMetadata);
            });
        }
        

        private async Task<Result<TResult, ProblemDetails>> ExecuteWithLogging<TResult>(Func<Task<Result<TResult, ProblemDetails>>> funcToExecute)
        {
            // TODO: Add request time measure
            var result = await funcToExecute();
            if(result.IsFailure)
                _logger.LogDataProviderRequestError($"Error executing provider request: '{result.Error.Detail}', status code: '{result.Error.Status}'");

            return result;
        }
        
        private readonly IDataProviderClient _dataProviderClient;
        private readonly string _baseUrl;
        private readonly ILogger<DataProvider> _logger;
    }
}