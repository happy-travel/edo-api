using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AvailabilityStorage : IAvailabilityStorage
    {
        public AvailabilityStorage(IDistributedFlow distributedFlow,
            IMemoryFlow memoryFlow,
            IDateTimeProvider dateTimeProvider,
            IOptions<DataProviderOptions> options)
        {
            _distributedFlow = distributedFlow;
            _memoryFlow = memoryFlow;
            _dateTimeProvider = dateTimeProvider;
            _providerOptions = options.Value;
        }


        public Task SaveResult(Guid searchId, DataProviders dataProvider, AvailabilityDetails details)
        {
            var timeStamp = _dateTimeProvider.UtcNow().Ticks;
            return SaveObject(searchId, dataProvider, new AvailabilityWithTimestamp(details, timeStamp));
        }


        public Task SetState(Guid searchId, DataProviders dataProvider, AvailabilitySearchState searchState)
            => SaveObject(searchId, dataProvider, searchState);


        public async Task<IEnumerable<ProviderData<AvailabilityResult>>> GetResult(Guid searchId)
        {
            var key = _memoryFlow.BuildKey(nameof(AvailabilityStorage), searchId.ToString());
            if (!_memoryFlow.TryGetValue(key, out List<(DataProviders DataProvider, AvailabilityWithTimestamp Result)> providerSearchResults))
            {
                providerSearchResults = (await GetProviderResults<AvailabilityWithTimestamp>(searchId))
                    .Where(t => !t.Result.Details.Equals(default))
                    .ToList();

                if ((await GetState(searchId)).TaskState == AvailabilitySearchTaskState.Completed)
                    _memoryFlow.Set(key, providerSearchResults, CacheExpirationTime);
            }

            return CombineAvailabilities(providerSearchResults);


            static IEnumerable<ProviderData<AvailabilityResult>> CombineAvailabilities(List<(DataProviders ProviderKey, AvailabilityWithTimestamp Availability)> availabilities)
            {
                if (availabilities == null || !availabilities.Any())
                    return Enumerable.Empty<ProviderData<AvailabilityResult>>();

                return availabilities
                    .OrderBy(r => r.Availability.TimeStamp)
                    .SelectMany(providerResults =>
                    {
                        var (providerKey, providerAvailability) = providerResults;
                        var details = providerAvailability.Details;
                        var availabilityResults = details
                            .Results
                            .Select(r =>
                            {
                                var result = new AvailabilityResult(providerAvailability.Details.AvailabilityId,
                                    r.AccommodationDetails,
                                    r.RoomContractSets);

                                return ProviderData.Create(providerKey, result);
                            })
                            .ToList();

                        return availabilityResults;
                    });
            }
        }


        public async Task<AvailabilitySearchState> GetState(Guid searchId)
        {
            var providerSearchStates = await GetProviderResults<AvailabilitySearchState>(searchId);
            var searchStates = providerSearchStates
                .Where(s => !s.Result.Equals(default))
                .Select(s => s.Result.TaskState)
                .ToHashSet();

            var totalResultsCount = GetResultsCount(providerSearchStates);
            var errors = GetErrors(providerSearchStates);

            if (searchStates.Count == 1)
                return AvailabilitySearchState.FromState(searchId, searchStates.Single(), totalResultsCount, errors);

            if (searchStates.Contains(AvailabilitySearchTaskState.Pending))
                return AvailabilitySearchState.PartiallyCompleted(searchId, totalResultsCount, errors);

            if (searchStates.All(s => s == AvailabilitySearchTaskState.Completed || s == AvailabilitySearchTaskState.Failed))
                return AvailabilitySearchState.Completed(searchId, totalResultsCount, errors);

            throw new ArgumentException($"Invalid tasks state: {string.Join(";", searchStates)}");


            static string GetErrors((DataProviders DataProvider, AvailabilitySearchState Result)[] states)
            {
                var errors = states
                    .Select(p => p.Result.Error)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToArray();

                return string.Join("; ", errors);
            }


            static int GetResultsCount((DataProviders DataProvider, AvailabilitySearchState Result)[] states)
            {
                return states.Sum(s => s.Result.ResultCount);
            }
        }


        private Task<(DataProviders DataProvider, TObject Result)[]> GetProviderResults<TObject>(Guid searchId)
        {
            var providerTasks = _providerOptions
                .EnabledProviders
                .Select(async p =>
                {
                    var key = BuildKey<TObject>(searchId, p);
                    return (
                        ProviderKey: p,
                        Object: await _distributedFlow.GetAsync<TObject>(key)
                    );
                })
                .ToArray();

            return Task.WhenAll(providerTasks);
        }


        private Task SaveObject<TObjectType>(Guid searchId, DataProviders dataProvider, TObjectType @object)
        {
            var key = BuildKey<TObjectType>(searchId, dataProvider);
            return _distributedFlow.SetAsync(key, @object, CacheExpirationTime);
        }


        private string BuildKey<TObjectType>(Guid searchId, DataProviders dataProvider)
            => _distributedFlow.BuildKey(nameof(AvailabilityStorage),
                searchId.ToString(),
                typeof(TObjectType).Name,
                dataProvider.ToString());


        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromMinutes(15);

        private readonly IDistributedFlow _distributedFlow;
        private readonly IMemoryFlow _memoryFlow;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly DataProviderOptions _providerOptions;
        
        
        private readonly struct AvailabilityWithTimestamp
        {
            [JsonConstructor]
            public AvailabilityWithTimestamp(AvailabilityDetails details, long timeStamp)
            {
                TimeStamp = timeStamp;
                Details = details;
            }
            
            public long TimeStamp { get; }
            public AvailabilityDetails Details { get; }
        }
    }
}