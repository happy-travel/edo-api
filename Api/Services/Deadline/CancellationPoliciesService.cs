using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Deadline
{
    public class CancellationPoliciesService : ICancellationPoliciesService
    {
        public CancellationPoliciesService(IDataProviderFactory dataProviderFactory,
            IMemoryFlow flow)
        {
            _dataProviderFactory = dataProviderFactory;
            _flow = flow;
        }


        public async Task<Result<DeadlineDetails, ProblemDetails>> GetDeadlineDetails(
            string availabilityId,
            string accommodationId,
            string tariffCode,
            DataProviders dataProvider,
            string languageCode)
        {
            var cacheKey = _flow.BuildKey(dataProvider.ToString(),
                accommodationId, availabilityId, tariffCode);
            if (_flow.TryGetValue(cacheKey, out DeadlineDetails result))
                return Result.Ok<DeadlineDetails, ProblemDetails>(result);

            Result<DeadlineDetails, ProblemDetails> response;
            switch (dataProvider)
            {
                case DataProviders.Netstorming:
                {
                    response = await GetDeadlineDetailsFromNetstorming(
                        accommodationId,
                        availabilityId,
                        tariffCode,
                        languageCode
                    );
                    break;
                }
                case DataProviders.Direct:
                case DataProviders.Illusions:
                    return ProblemDetailsBuilder.Fail<DeadlineDetails>($"{nameof(dataProvider)}:{dataProvider} hasn't implemented yet");
                case DataProviders.Unknown:
                default: return ProblemDetailsBuilder.Fail<DeadlineDetails>("Unknown contract type");
            }

            if (response.IsSuccess)
                _flow.Set(cacheKey, response.Value, _expirationPeriod);
            return response;
        }


        private Task<Result<DeadlineDetails, ProblemDetails>> GetDeadlineDetailsFromNetstorming(
            string accommodationId, string availabilityId, string agreementCode, string languageCode)
        {
            // TODO: replace with conditional data provider
            var dataProvider = _dataProviderFactory.Get(DataProviders.Netstorming);
            return dataProvider.GetDeadline(accommodationId, availabilityId, agreementCode, languageCode);
        }

        private readonly TimeSpan _expirationPeriod = TimeSpan.FromHours(1);
        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly IMemoryFlow _flow;
    }
}