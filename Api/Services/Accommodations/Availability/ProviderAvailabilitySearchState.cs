using System;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public readonly struct ProviderAvailabilitySearchState
    {
        [JsonConstructor]
        private ProviderAvailabilitySearchState(Guid id, AvailabilitySearchTaskState taskState, int resultCount = 0, string error = null)
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


        public static ProviderAvailabilitySearchState Failed(Guid id, string error)
            => new ProviderAvailabilitySearchState(id, AvailabilitySearchTaskState.Failed, error: error);


        public static ProviderAvailabilitySearchState Completed(Guid id, int resultCount, string error = null)
            => new ProviderAvailabilitySearchState(id, AvailabilitySearchTaskState.Completed, resultCount, error);


        public static ProviderAvailabilitySearchState PartiallyCompleted(Guid id, int resultCount, string error)
            => new ProviderAvailabilitySearchState(id, AvailabilitySearchTaskState.PartiallyCompleted, resultCount);


        public static ProviderAvailabilitySearchState Pending(Guid id) => new ProviderAvailabilitySearchState(id, AvailabilitySearchTaskState.Pending);


        public static ProviderAvailabilitySearchState FromState(Guid id, AvailabilitySearchTaskState taskState, int resultCount, string error)
            => new ProviderAvailabilitySearchState(id, taskState, resultCount, error);


        public bool Equals(WideAvailabilitySearchState other)
            => Id.Equals(other.Id) && TaskState == other.TaskState && ResultCount == other.ResultCount && Error == other.Error;


        public override bool Equals(object obj) => obj is WideAvailabilitySearchState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, (int) TaskState, ResultCount, Error);
    }
}