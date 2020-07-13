using System.Collections.Generic;
using System.Linq;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct AvailabilityResult
    {
        [JsonConstructor]
        public AvailabilityResult(string availabilityId, SlimAccommodationDetails accommodationDetails, List<RoomContractSet> roomContractSets)
        {
            AvailabilityId = availabilityId;
            AccommodationDetails = accommodationDetails;
            RoomContractSets = roomContractSets ?? new List<RoomContractSet>();
        }


        public AvailabilityResult(AvailabilityResult result, List<RoomContractSet> roomContractSets)
            : this(result.AvailabilityId, result.AccommodationDetails, roomContractSets)
        { }
        
        /// <summary>
        /// Id of availability search
        /// </summary>
        public string AvailabilityId { get; }
        
        /// <summary>
        /// Accommodation data
        /// </summary>
        public SlimAccommodationDetails AccommodationDetails { get; }
        
        /// <summary>
        /// List of available room contracts sets
        /// </summary>
        public List<RoomContractSet> RoomContractSets { get; }
    }
}