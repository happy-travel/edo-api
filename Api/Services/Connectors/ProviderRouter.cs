using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class ProviderRouter : IProviderRouter
    {
        public ProviderRouter(IDataProviderFactory dataProviderFactory)
        {
            _dataProviderFactory = dataProviderFactory;
        }


        public async Task<Result<CombinedAvailabilityDetails>> GetAvailability(List<DataProviders> dataProviders, AvailabilityRequest availabilityRequest, RequestMetadata requestMetadata)
        {
            var results = await GetResultsFromConnectors();

            var failedResults = results
                .Where(r => r.Result.IsFailure)
                .ToList();

            if (results.Count != 0 && failedResults.Count == results.Count)
            {
                var errorMessage = string.Join("; ", failedResults.Select(r => r.Result.Error.Detail).Distinct());
                if (string.IsNullOrWhiteSpace(errorMessage))
                    errorMessage = "No error details provided.";

                return Result.Fail<CombinedAvailabilityDetails>($"A {nameof(ProviderRouter)} error occured. Details: '{errorMessage}'");
            }

            var succeededResults = results
                .Where(r => r.Result.IsSuccess)
                .Select(r => (r.ProviderKey, r.Result.Value))
                .ToList();

            return Result.Ok(CombineAvailabilities(succeededResults));


            async Task<List<(DataProviders ProviderKey, Result<AvailabilityDetails, ProblemDetails> Result)>> GetResultsFromConnectors()
            {
                var providers = dataProviders != null && dataProviders.Any()
                    ? _dataProviderFactory.Get(dataProviders)
                    // TODO: remove this after filling database with locations,which always have dataProviders
                    : _dataProviderFactory.GetAll();

                var getAvailabilityTasks = providers.Select(async providerInfo =>
                {
                    var result = await providerInfo.Provider.GetAvailability(availabilityRequest, requestMetadata);
                    return (providerInfo.Key, result);
                }).ToList();

                await Task.WhenAll(getAvailabilityTasks);

                return getAvailabilityTasks
                    .Select(t => t.Result)
                    .ToList();
            }
        }


        public Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(DataProviders dataProvider, string accommodationId,
            string availabilityId, RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetAvailability(availabilityId, accommodationId, requestMetadata);
        }


        public Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> GetExactAvailability(DataProviders dataProvider,
            string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetExactAvailability(availabilityId, roomContractSetId, requestMetadata);
        }


        public Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(DataProviders dataProvider, string id, RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetAccommodation(id, requestMetadata);
        }


        public Task<Result<BookingDetails, ProblemDetails>> Book(DataProviders dataProvider, BookingRequest request, RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.Book(request, requestMetadata);
        }


        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(DataProviders dataProvider, string referenceCode, RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.CancelBooking(referenceCode, requestMetadata);
        }


        public Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(DataProviders dataProvider, string availabilityId,
            Guid roomContractSetId, RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetDeadline(availabilityId, roomContractSetId, requestMetadata);
        }
        
        
        public Task<Result<BookingDetails, ProblemDetails>> GetBookingDetails(DataProviders dataProvider, string referenceCode,
            RequestMetadata requestMetadata)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetBookingDetails(referenceCode, requestMetadata);
        }


        private CombinedAvailabilityDetails CombineAvailabilities(List<(DataProviders ProviderKey, AvailabilityDetails Availability)> availabilities)
        {
            if (availabilities == null || !availabilities.Any())
                return new CombinedAvailabilityDetails(default, default, default, default, default);

            var firstResult = availabilities.First().Availability;

            var results = availabilities
                .SelectMany(providerResults =>
                {
                    var (providerKey, providerAvailability) = providerResults;
                    var availabilityResults = providerAvailability
                        .Results
                        .Select(r =>
                        {
                            var result = new AvailabilityResult(providerAvailability.AvailabilityId,
                                r.AccommodationDetails,
                                r.RoomContractSets);

                            return ProviderData.Create(providerKey, result);
                        })
                        .ToList();

                    return availabilityResults;
                })
                .ToList();

            var processed = availabilities.Sum(a => a.Availability.NumberOfProcessedAccommodations);
            return new CombinedAvailabilityDetails(firstResult.NumberOfNights, firstResult.CheckInDate, firstResult.CheckOutDate, processed, results);
        }


        private readonly IDataProviderFactory _dataProviderFactory;
    }
}