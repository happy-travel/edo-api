using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.UnitTests.Utility;
using Microsoft.Extensions.Options;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.AvailabilityStorageTests
{
    public class TaskStateCalculation
    {
        // TODO Rewrite tests during NIJO-787
        // [Theory]
        // [InlineData(DataProviders.Illusions, AvailabilitySearchTaskState.Completed)]
        // [InlineData(DataProviders.Netstorming, AvailabilitySearchTaskState.Failed)]
        // [InlineData(DataProviders.Etg, AvailabilitySearchTaskState.Pending)]
        // public async Task One_provider_search_should_return_provider_state(DataProviders dataProvider, AvailabilitySearchTaskState providerSearchState)
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("45a364fb-33be-4115-97fe-c94090d86452");
        //     var providerState = AvailabilitySearchState.FromState(searchId, providerSearchState, 10, string.Empty);
        //
        //     await storage.SetState(searchId, dataProvider, providerState);
        //
        //     var calculatedState = await storage.GetState(searchId);
        //     Assert.Equal(providerSearchState, calculatedState.TaskState);
        // }
        //
        //
        // [Theory]
        // [InlineData(AvailabilitySearchTaskState.Completed)]
        // [InlineData(AvailabilitySearchTaskState.Failed)]
        // [InlineData(AvailabilitySearchTaskState.Pending)]
        // public async Task Should_return_same_state_when_provider_states_equal(AvailabilitySearchTaskState searchTaskState)
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("1929875f-275f-46ec-84b7-d32f6a4f30d8");
        //     var providerSearchStates = new Dictionary<DataProviders, AvailabilitySearchTaskState>
        //     {
        //         {DataProviders.Etg, searchTaskState},
        //         {DataProviders.Illusions, searchTaskState},
        //         {DataProviders.Netstorming, searchTaskState}
        //     };
        //
        //     await SetProviderStates(providerSearchStates, searchId, storage);
        //
        //     var calculatedState = await storage.GetState(searchId);
        //     Assert.Equal(searchTaskState, calculatedState.TaskState);
        // }
        //
        //
        // [Fact]
        // public async Task Should_return_completed_when_all_searches_finished_or_failed()
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("91c56a8a-cba1-4832-8251-030ac51aee77");
        //     var providerSearchStates = new Dictionary<DataProviders, AvailabilitySearchTaskState>
        //     {
        //         {DataProviders.Etg, AvailabilitySearchTaskState.Completed},
        //         {DataProviders.Illusions, AvailabilitySearchTaskState.Failed},
        //         {DataProviders.Netstorming, AvailabilitySearchTaskState.Failed}
        //     };
        //
        //     await SetProviderStates(providerSearchStates, searchId, storage);
        //     
        //     var calculatedState = await storage.GetState(searchId);
        //     Assert.Equal(AvailabilitySearchTaskState.Completed, calculatedState.TaskState);
        // }
        //
        //
        // [Fact]
        // public async Task Should_return_partially_completed_when_one_connector_is_pending()
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("815379cb-419f-465b-b671-e081c73876a8");
        //     var providerSearchStates = new Dictionary<DataProviders, AvailabilitySearchTaskState>
        //     {
        //         {DataProviders.Etg, AvailabilitySearchTaskState.Completed},
        //         {DataProviders.Illusions, AvailabilitySearchTaskState.Pending},
        //         {DataProviders.Netstorming, AvailabilitySearchTaskState.Failed}
        //     };
        //
        //     await SetProviderStates(providerSearchStates, searchId, storage);
        //     
        //     var calculatedState = await storage.GetState(searchId);
        //     Assert.Equal(AvailabilitySearchTaskState.PartiallyCompleted, calculatedState.TaskState);
        // }
        //
        //
        // [Fact]
        // public async Task Should_get_states_only_for_enabled_connectors()
        // {
        //     var storage = AvailabilityStorageUtils.CreateEmptyStorage<AvailabilitySearchState>(_providerOptions);
        //     var searchId = new Guid("7630f5fb-6773-473d-8cd8-e702609ca514");
        //     var providerSearchStates = new Dictionary<DataProviders, AvailabilitySearchTaskState>
        //     {
        //         {DataProviders.Direct, AvailabilitySearchTaskState.Completed},
        //         {DataProviders.Illusions, AvailabilitySearchTaskState.Failed},
        //         {DataProviders.Netstorming, AvailabilitySearchTaskState.Failed}
        //     };
        //     // Direct is disabled here
        //     Assert.DoesNotContain(_providerOptions.Value.EnabledProviders, dp => dp == DataProviders.Direct);
        //
        //     await SetProviderStates(providerSearchStates, searchId, storage);
        //
        //     var calculatedState = await storage.GetState(searchId);
        //     Assert.Equal(AvailabilitySearchTaskState.Failed, calculatedState.TaskState);
        // }
        //
        //
        // private static async Task SetProviderStates(Dictionary<DataProviders, AvailabilitySearchTaskState> providerSearchStates, Guid searchId, IAvailabilityStorage storage)
        // {
        //     foreach (var providerSearchState in providerSearchStates)
        //     {
        //         var providerState = AvailabilitySearchState.FromState(searchId, providerSearchState.Value, 10, string.Empty);
        //         await storage.SetState(searchId, providerSearchState.Key, providerState);
        //     }
        // }
        //
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