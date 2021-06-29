using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.SupplierConnectors;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class SupplierConnector : ISupplierConnector
    {
        public SupplierConnector(IConnectorClient connectorClient, string baseUrl, ILogger<SupplierConnector> logger)
        {
            _connectorClient = connectorClient;
            _baseUrl = baseUrl;
            _logger = logger;
        }
        
        
        public Task<Result<Availability, ProblemDetails>> GetAvailability(AvailabilityRequest request, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Post<AvailabilityRequest, Availability>(
                    new Uri(_baseUrl + "accommodations/availabilities", UriKind.Absolute), request, languageCode);
            });
        }


        public Task<Result<AccommodationAvailability, ProblemDetails>> GetAvailability(string availabilityId,
            string accommodationId, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Post<AccommodationAvailability>(
                    new Uri(_baseUrl + "accommodations/" + accommodationId + "/availabilities/" + availabilityId, UriKind.Absolute), languageCode);
            });
        }
        
        
        public Task<Result<RoomContractSetAvailability?, ProblemDetails>> GetExactAvailability(string availabilityId, Guid roomContractSetId, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Post<RoomContractSetAvailability?>(
                    new Uri($"{_baseUrl}accommodations/availabilities/{availabilityId}/room-contract-sets/{roomContractSetId}", UriKind.Absolute), languageCode);
            });
        }


        public Task<Result<Deadline, ProblemDetails>> GetDeadline(string availabilityId, Guid roomContractSetId, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                var uri = new Uri($"{_baseUrl}accommodations/availabilities/{availabilityId}/room-contract-sets/{roomContractSetId}/deadline", UriKind.Absolute);
                return _connectorClient.Get<Deadline>(uri, languageCode);
            });
        }


        public Task<Result<Booking, ProblemDetails>> Book(BookingRequest request, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Post<BookingRequest, Booking>(
                    new Uri(_baseUrl + "accommodations/bookings", UriKind.Absolute),
                    request, languageCode);
            });
        }


        public Task<Result<Unit, ProblemDetails>> CancelBooking(string referenceCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Post(new Uri(_baseUrl + "accommodations/bookings/" + referenceCode + "/cancel",
                    UriKind.Absolute));
            });
        }


        public Task<Result<Booking, ProblemDetails>> GetBookingDetails(string referenceCode, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Get<Booking>(
                    new Uri(_baseUrl + "accommodations/bookings/" + referenceCode,
                        UriKind.Absolute), languageCode);
            });
        }


        public Task<Result<Booking, ProblemDetails>> ProcessAsyncResponse(Stream stream)
        {
            return ExecuteWithLogging(() =>
            {
                return _connectorClient.Post<Booking>(new Uri(_baseUrl + "bookings/response", UriKind.Absolute), stream);
            });
        }
        

        private async Task<Result<TResult, ProblemDetails>> ExecuteWithLogging<TResult>(Func<Task<Result<TResult, ProblemDetails>>> funcToExecute)
        {
            var sw = Stopwatch.StartNew();
            var result = await funcToExecute();
            sw.Stop();
            _logger.LogSupplierConnectorRequestDuration(_baseUrl, sw.ElapsedMilliseconds);
            
            if (result.IsFailure)
                _logger.LogSupplierConnectorRequestError(_baseUrl, result.Error.Detail, result.Error.Status);

            return result;
        }
        
        private readonly IConnectorClient _connectorClient;
        private readonly string _baseUrl;
        private readonly ILogger<SupplierConnector> _logger;
    }
}