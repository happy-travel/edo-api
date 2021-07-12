using HappyTravel.Edo.Api.Models.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public static class RoomContractSetExtensions
    {
        public static RoomContractSet ApplySearchSettings(this RoomContractSet rs, bool isSupplierVisible, bool isDirectContractsVisible) 
            => new(rs.Id,
            rs.Rate,
            rs.Deadline,
            rs.Rooms,
            rs.IsAdvancePurchaseRate,
            isSupplierVisible
                ? rs.Supplier
                : null,
            rs.Tags,
            isDirectContract: isDirectContractsVisible && rs.IsDirectContract);
    }
}