using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.UnitTests.Utility;
using Microsoft.Extensions.Options;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.AvailabilityStorageTests
{
    public class ResultCountCalculation
    {
        // TODO Rewrite tests during NIJO-787
        // [Fact]
        // public async Task Provider_search_results_count_should_sum()
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("c273b8eb-5351-424a-a10b-910ed755f6d5");
        //     var providerStates = new Dictionary<DataProviders, AvailabilitySearchState>
        //     {
        //         {DataProviders.Etg, AvailabilitySearchState.Completed(searchId, 10)},
        //         {DataProviders.Netstorming, AvailabilitySearchState.Completed(searchId, 15)},
        //         {DataProviders.Illusions, AvailabilitySearchState.Completed(searchId, 144)}
        //     };
        //
        //     foreach (var providerState in providerStates)
        //         await storage.SetState(searchId, providerState.Key, providerState.Value);
        //
        //     var calculatedState = await storage.GetState(searchId);
        //     Assert.Equal(169, calculatedState.ResultCount);
        // }
        //
        // private readonly IOptions<DataProviderOptions> _providerOptions = Options.Create(new DataProviderOptions
        // {
        //     EnabledProviders = new List<DataProviders>
        //     {
        //         DataProviders.Etg,
        //         DataProviders.Illusions,
        //         DataProviders.Netstorming
        //     }
        // });
    }
}