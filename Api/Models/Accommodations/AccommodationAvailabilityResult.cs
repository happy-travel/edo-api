using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct AccommodationAvailabilityResult
    {
        [JsonConstructor]
        public AccommodationAvailabilityResult(Guid id,
            long timestamp,
            string availabilityId,
            SlimAccommodation accommodation,
            List<RoomContractSet> roomContractSets,
            string duplicateReportId,
            decimal minPrice,
            decimal maxPrice,
            DateTime checkInDate,
            DateTime checkOutDate, 
            string htId)
        {
            Id = id;
            Timestamp = timestamp;
            AvailabilityId = availabilityId;
            Accommodation = accommodation;
            RoomContractSets = roomContractSets;
            DuplicateReportId = duplicateReportId;
            MinPrice = minPrice;
            MaxPrice = maxPrice;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            HtId = htId;
        }
        
        public Guid Id { get; }
        public long Timestamp { get; }
        public string AvailabilityId { get; }
        public SlimAccommodation Accommodation { get; }
        public List<RoomContractSet> RoomContractSets { get; }
        public string DuplicateReportId { get; }
        public decimal MinPrice { get; }
        public decimal MaxPrice { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public string HtId { get; }


        public bool Equals(AccommodationAvailabilityResult other)
        {
            return Id.Equals(other.Id) && Timestamp == other.Timestamp && AvailabilityId == other.AvailabilityId &&
                Accommodation.Equals(other.Accommodation) && Equals(RoomContractSets, other.RoomContractSets) &&
                DuplicateReportId == other.DuplicateReportId && MinPrice == other.MinPrice && MaxPrice == other.MaxPrice &&
                HtId == other.HtId;
        }


        public override bool Equals(object obj) => obj is AccommodationAvailabilityResult other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Id, Timestamp, AvailabilityId, Accommodation, RoomContractSets, DuplicateReportId, MinPrice, MaxPrice);

    }
}