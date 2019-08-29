using System;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Availabilities;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AvailabilityResultsCache : IAvailabilityResultsCache
    {
        public AvailabilityResultsCache(IMemoryFlow flow)
        {
            _flow = flow;
        }

        public Task Set(DataProviderAvailabilityResponse dataProviderAvailabilityResponse)
        {
            _flow.Set(
                _flow.BuildKey(KeyPrefix, dataProviderAvailabilityResponse.AvailabilityId.ToString()),
                dataProviderAvailabilityResponse,
                ExpirationPeriod);

            return Task.CompletedTask;
        }

        public Task<DataProviderAvailabilityResponse> Get(int id)
        {
            _flow.TryGetValue<DataProviderAvailabilityResponse>(_flow.BuildKey(KeyPrefix, id.ToString()),
                out var availabilityResponse);
            return Task.FromResult(availabilityResponse);
        }
        
        private const string KeyPrefix = nameof(DataProviderAvailabilityResponse) + "AvailabilityResults";
        private static readonly TimeSpan ExpirationPeriod = TimeSpan.FromHours(1);
        private readonly IMemoryFlow _flow;
    }
}