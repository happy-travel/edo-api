using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public readonly struct WideAvailabilitySearchState
    {
        [JsonConstructor]
        private WideAvailabilitySearchState(Guid id, AvailabilitySearchTaskState taskState, int resultCount = 0, string error = null)
        {
            Id = id;
            TaskState = taskState;
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
        /// Result count
        /// </summary>
        public int ResultCount { get; }
        
        /// <summary>
        /// Error message. Filled only for failed tasks
        /// </summary>
        public string Error { get; }


        public static WideAvailabilitySearchState FromProviderStates(Guid searchId, IEnumerable<(DataProviders, ProviderAvailabilitySearchState)> searchStates)
        {
            var statesDictionary = searchStates
                .ToDictionary(s => s.Item1, s => s.Item2);
            
            var overallState = CalculateOverallState(statesDictionary);
            var totalResultsCount = GetResultsCount(statesDictionary);
            var errors = GetErrors(statesDictionary);
            
            return new WideAvailabilitySearchState(searchId, overallState, totalResultsCount, errors);


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


        public bool Equals(WideAvailabilitySearchState other)
            => Id.Equals(other.Id) && TaskState == other.TaskState && ResultCount == other.ResultCount && Error == other.Error;


        public override bool Equals(object obj) => obj is WideAvailabilitySearchState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, (int) TaskState, ResultCount, Error);
    }
}