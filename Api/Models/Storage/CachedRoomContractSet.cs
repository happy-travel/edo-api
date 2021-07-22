using System;
using System.Collections.Generic;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.SuppliersCatalog;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedRoomContractSet
    {
        public Guid Id { get; init; }
        public CachedRate Rate { get; init; }
        public Deadline Deadline { get; init; }
        public bool IsAdvancePurchaseRate { get; init; }
        public List<CachedRoomContract> Rooms { get; init; }
        public Suppliers? Supplier { get; init; }
        public List<string> Tags { get; init; }
        public bool IsDirectContract { get; init; }
    }
}