using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public class MultiDataProviderAvailabilityManager : IMultiDataProviderAvailabilityManager
    {
        public MultiDataProviderAvailabilityManager(IDataProviderFactory dataProviderFactory)
        {
            _dataProviderFactory = dataProviderFactory;
        }


        public async Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode)
        {
            var results = await GetResultsFromConnectors();

            var failedResults = results
                .Where(r => r.IsFailure)
                .ToList();

            if (failedResults.Count == results.Count)
            {
                var errorMessage = string.Join("; ", failedResults.Select(r => r.Error).Distinct());
                return ProblemDetailsBuilder.Fail<AvailabilityDetails>(errorMessage);
            }

            var successResults = results
                .Where(r => r.IsSuccess)
                .Select(r => r.Value)
                .ToList();

            return Result.Ok<AvailabilityDetails, ProblemDetails>(CombineAvailabilities(successResults));


            async Task<List<Result<AvailabilityDetails, ProblemDetails>>> GetResultsFromConnectors()
            {
                var getAvailabilityTasks = _dataProviderFactory
                    .GetAll()
                    .Select(dp => dp.GetAvailability(availabilityRequest, languageCode))
                    .ToArray();

                await Task.WhenAll(getAvailabilityTasks);
                return getAvailabilityTasks
                    .Select(t => t.Result)
                    .ToList();
            }
        }


        private AvailabilityDetails CombineAvailabilities(List<AvailabilityDetails> results)
        {
            // TODO: Add results combination
            return results.Single();
        }


        private readonly IDataProviderFactory _dataProviderFactory;
    }
}