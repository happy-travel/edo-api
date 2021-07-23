using System.Collections.Generic;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;

namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedRoomContract
    {
        public BoardBasisTypes BoardBasis { get; init; }
        public string MealPlan { get; init; }
        public int ContractTypeCode { get; init; }
        public bool IsAvailableImmediately { get; init; }
        public bool IsDynamic { get; init; }
        public string ContractDescription { get; init; }
        public CachedRate Rate { get; init; }
        public List<KeyValuePair<string, string>> Remarks { get; init; }
        public int AdultsNumber { get; init; }
        public List<int> ChildrenAges { get; init; }
        public bool IsExtraBedNeeded { get; init; }
        public Deadline Deadline { get; init; }
        public bool IsAdvancePurchaseRate { get; init; }
        public List<CachedDailyRate> DailyRoomRates { get; init; }
        public RoomTypes Type { get; init; }
    }
}