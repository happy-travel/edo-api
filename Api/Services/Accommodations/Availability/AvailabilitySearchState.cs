using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public readonly struct AvailabilitySearchState
    {
        [JsonConstructor]
        private AvailabilitySearchState(Guid id, AvailabilitySearchTaskState taskState, Dictionary<DataProviders, ProviderAvailabilitySearchState> providerStates, int resultCount = 0, string error = null)
        {
            Id = id;
            TaskState = taskState;
            ProviderStates = providerStates;
            ResultCount = resultCount;
            Error = error;
        }


        /// <summary>
        /// Search id
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        /// Search task state
        /// </summary>
        public AvailabilitySearchTaskState TaskState { get; }

        /// <summary>
        /// Provider search states
        /// </summary>
        public Dictionary<DataProviders, ProviderAvailabilitySearchState> ProviderStates { get; }

        /// <summary>
        /// Result count
        /// </summary>
        public int ResultCount { get; }
        
        /// <summary>
        /// Error message. Filled only for failed tasks
        /// </summary>
        public string Error { get; }


        public static AvailabilitySearchState FromProviderStates(Guid searchId, Dictionary<DataProviders, ProviderAvailabilitySearchState> providerSearchStates)
        {
            var overallState = CalculateOverallState(providerSearchStates);
            var totalResultsCount = GetResultsCount(providerSearchStates);
            var errors = GetErrors(providerSearchStates);
            
            return new AvailabilitySearchState(searchId, overallState, providerSearchStates, totalResultsCount, errors);


            static AvailabilitySearchTaskState CalculateOverallState(Dictionary<DataProviders, ProviderAvailabilitySearchState> providerSearchStates)
            {
                var searchStates = providerSearchStates
                    .Select(r => r.Value.TaskState)
                    .ToHashSet();

                if (searchStates.Count == 1)
                    return searchStates.Single();

                if (searchStates.Contains(AvailabilitySearchTaskState.Pending))
                    return AvailabilitySearchTaskState.PartiallyCompleted;

                if (searchStates.All(s => s == AvailabilitySearchTaskState.Completed || s == AvailabilitySearchTaskState.Failed))
                    return AvailabilitySearchTaskState.Completed;
                
                throw new ArgumentException($"Invalid tasks state: {string.Join(";", searchStates)}");
            }


            static string GetErrors(Dictionary<DataProviders, ProviderAvailabilitySearchState> states)
            {
                var errors = states
                    .Select(p => p.Value.Error)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToArray();

                return string.Join("; ", errors);
            }


            static int GetResultsCount(Dictionary<DataProviders, ProviderAvailabilitySearchState> states)
            {
                return states.Sum(s => s.Value.ResultCount);
            }
        }


        public bool Equals(AvailabilitySearchState other)
            => Id.Equals(other.Id) && TaskState == other.TaskState && ResultCount == other.ResultCount && Error == other.Error;


        public override bool Equals(object obj) => obj is AvailabilitySearchState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, (int) TaskState, ResultCount, Error);
    }
}