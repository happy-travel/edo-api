using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
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


        public async Task<Result<CombinedAvailabilityDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode)
        {
            var results = await GetResultsFromConnectors();

            var failedResults = results
                .Where(r => r.Result.IsFailure)
                .ToList();

            if (failedResults.Count == results.Count)
            {
                var errorMessage = string.Join("; ", failedResults.Select(r => r.Result.Error.Detail).Distinct());
                return Result.Fail<CombinedAvailabilityDetails>(errorMessage);
            }

            var succeededResults = results
                .Where(r => r.Result.IsSuccess)
                .Select(r=> (r.ProviderKey, r.Result.Value))
                .ToList();

            return Result.Ok(CombineAvailabilities(succeededResults));

            
            async Task<List<(DataProviders ProviderKey, Result<AvailabilityDetails, ProblemDetails> Result)>> GetResultsFromConnectors()
            {
                var getAvailabilityTasks = _dataProviderFactory
                    .GetAll()
                    .Select(async providerInfo =>
                    {
                        var result = await providerInfo.Provider.GetAvailability(availabilityRequest, languageCode);
                        return (providerInfo.Key, result);
                    })
                    .ToList();
                    
                await Task.WhenAll(getAvailabilityTasks);

                return getAvailabilityTasks
                    .Select(t => t.Result)
                    .ToList();
            }
        }


        public Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(DataProviders dataProvider, string accommodationId, long availabilityId, string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetAvailability(availabilityId, accommodationId, languageCode);
        }


        public Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(DataProviders dataProvider, long availabilityId, Guid agreementId, string languageCode)
        {
            var provider = _dataProviderFactory.Get(dataProvider);
            return provider.GetExactAvailability(availabilityId, agreementId, languageCode);
        }


        private CombinedAvailabilityDetails CombineAvailabilities(List<(DataProviders ProviderKey, AvailabilityDetails Availability)> availabilities)
        {
            var firstResult = availabilities.First().Availability;

            var results = availabilities
                .SelectMany(providerResults =>
                {
                    var (providerKey, providerAvailability) = providerResults;
                    return providerAvailability
                        .Results
                        .Select(r =>
                        {
                            var result = new AvailabilityResult(providerAvailability.AvailabilityId,
                                r.AccommodationDetails,
                                r.Agreements);

                            return ProviderData.Create(providerKey, result);
                        })
                        .ToList();
                })
                .ToList();
            
            return new CombinedAvailabilityDetails(firstResult.NumberOfNights,
                firstResult.CheckInDate,
                firstResult.CheckOutDate,
                results);
        }

        private readonly IDataProviderFactory _dataProviderFactory;
    }
}