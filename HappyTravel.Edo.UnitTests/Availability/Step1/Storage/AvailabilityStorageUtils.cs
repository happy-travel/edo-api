using System;
using System.Threading;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace HappyTravel.Edo.UnitTests.Availability.Step1.Storage
{
    internal static class AvailabilityStorageUtils
    {
        public static IAvailabilityStorage CreateEmptyStorage(IOptions<DataProviderOptions> providerOptions)
        {
            var memoryFlow = new MemoryFlow(new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var distributedFlowMock = new Mock<IDistributedFlow>();

            distributedFlowMock
                .Setup(f => f.Options)
                .Returns(new FlowOptions());

            distributedFlowMock
                .Setup(f => f.SetAsync(It.IsAny<string>(), It.IsAny<AvailabilitySearchState>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback<string, AvailabilitySearchState, TimeSpan, CancellationToken>((key, value, timeSpan, _) =>
                {
                    memoryFlow.Set(key, value, timeSpan);
                });

            distributedFlowMock
                .Setup(f => f.GetAsync<AvailabilitySearchState>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((key, _) =>
                {
                    memoryFlow.TryGetValue<AvailabilitySearchState>(key, out var value);
                    return Task.FromResult(value);
                });
                    
            
            return new AvailabilityStorage(distributedFlowMock.Object,
                Mock.Of<IMemoryFlow>(), 
                providerOptions);
        }
    }
}