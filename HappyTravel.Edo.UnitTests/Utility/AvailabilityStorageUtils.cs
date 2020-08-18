using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace HappyTravel.Edo.UnitTests.Utility
{
    internal static class AvailabilityStorageUtils
    {
        public static IMultiProviderAvailabilityStorage CreateEmptyStorage<TObject>(IOptions<DataProviderOptions> providerOptions)
        {
            var memoryFlow = new MemoryFlow(new DiagnosticListener("test"), new MemoryCache(Options.Create(new MemoryCacheOptions())));
            var distributedFlowMock = new Mock<IDistributedFlow>();

            distributedFlowMock
                .Setup(f => f.Options)
                .Returns(new FlowOptions());

            distributedFlowMock
                .Setup(f => f.SetAsync(It.IsAny<string>(), It.IsAny<TObject>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Callback<string, TObject, TimeSpan, CancellationToken>((key, value, timeSpan, _) =>
                {
                    memoryFlow.Set(key, value, timeSpan);
                });

            distributedFlowMock
                .Setup(f => f.GetAsync<TObject>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((key, _) =>
                {
                    memoryFlow.TryGetValue<TObject>(key, out var value);
                    return Task.FromResult(value);
                });
                    
            
            return new MultiProviderAvailabilityStorage(distributedFlowMock.Object,
                memoryFlow);
        }
    }
}