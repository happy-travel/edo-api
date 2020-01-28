using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
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
        
        
        public Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest request, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<AvailabilityRequest, AvailabilityDetails>(
                    new Uri(_baseUrl + "availabilities/accommodations", UriKind.Absolute), request, languageCode);
            });
        }


        public Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailability(long availabilityId,
            string accommodationId, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<SingleAccommodationAvailabilityDetails>(
                    new Uri(_baseUrl + "accommodations/" + accommodationId + "/availabilities/" + availabilityId, UriKind.Absolute), languageCode);
            });
        }


        public Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(string accommodationId, string availabilityId, string agreementCode, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                var uri = new Uri($"{_baseUrl}accommodations/{accommodationId}/deadline/{availabilityId}/{agreementCode}", UriKind.Absolute);
                return _dataProviderClient.Get<DeadlineDetails>(uri, languageCode);
            });
        }


        public Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(string accommodationId, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Get<AccommodationDetails>(
                    new Uri($"{_baseUrl}accommodations/{accommodationId}", UriKind.Absolute), languageCode);
            });
        }


        public Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(long availabilityId, Guid agreementId, string languageCode)
        {
            return ExecuteWithLogging(() =>
            {
                return _dataProviderClient.Post<SingleAccommodationAvailabilityDetailsWithDeadline>(
                    new Uri($"{_baseUrl}accommodations/availabilities/{availabilityId}/agreements/{agreementId}", UriKind.Absolute), languageCode);
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