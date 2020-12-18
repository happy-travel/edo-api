using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public static class BookingPermissionHelper
    {
        public static bool DoesAgentHavePermissions(Booking booking, AgentContext agent)
        {
            if (booking.AgencyId != agent.AgencyId)
                return false;

            if (booking.AgentId == agent.AgentId)
                return true;

            return agent.InAgencyPermissions.HasFlag(InAgencyPermissions.AgencyBookingsManagement);
        }
    }
}