using System;
using System.Collections.Generic;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedAccommodationAvailabilityResult
    {
        public Guid SearchId { get; init; }
        public Suppliers Supplier { get; init; }
        public DateTime Created { get; init; }
        public long Timestamp { get; init; }
        public string AvailabilityId { get; init; }
        public List<CachedRoomContractSet> RoomContractSets { get; init; }
        public decimal MinPrice { get; init; }
        public decimal MaxPrice { get; init; }
        public DateTime CheckInDate { get; init; }
        public DateTime CheckOutDate { get; init; }
        public string HtId { get; init; }
        public string SupplierAccommodationCode { get; init; }
    }
}