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
    public class ErrorAggregation
    {
        // TODO Rewrite tests during NIJO-787
        // [Fact]
        // public async Task Search_state_should_contain_all_errors()
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("ae05b78f-4488-4845-9f7d-bad3d4cd177e");
        //     var errors = new[] {"Failed to connect", "Failed to fetch", "Server error"};
        //     var providerStates = new Dictionary<DataProviders, AvailabilitySearchState>
        //     {
        //         {DataProviders.Etg, AvailabilitySearchState.Failed(searchId, errors[0])},
        //         {DataProviders.Netstorming, AvailabilitySearchState.Failed(searchId, errors[1])},
        //         {DataProviders.Illusions, AvailabilitySearchState.Failed(searchId, errors[2])}
        //     };
        //
        //     foreach (var providerState in providerStates)
        //         await storage.SetState(searchId, providerState.Key, providerState.Value);
        //
        //     var calculatedState = await storage.GetState(searchId);
        //
        //     foreach (var error in errors)
        //         Assert.Contains(error, calculatedState.Error);
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