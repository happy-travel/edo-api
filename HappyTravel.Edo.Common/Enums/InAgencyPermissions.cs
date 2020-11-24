using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    [Flags]
    public enum InAgencyPermissions
    {
        None = 1,
        AgentInvitation = 2,
        AccommodationAvailabilitySearch = 4,
        AccommodationBooking = 8,
        PermissionManagement = 16,
        ObserveMarkup = 32,
        ObserveAgents = 64,
        ObserveBalance = 128,
        ObservePaymentHistory = 256,
        AgencyToChildTransfer = 512,
        ReceiveBookingSummary = 1024,
        AgencyBookingsManagement = 2048,
        AgentStatusManagement = 4096,
        ObserveAgencyInvitations = 8192,
        AgencyImagesManagement = 16384,
        ObserveCounterpartyContract = 32768,
        // All = 01111111111111111111111111111110. First bit is 0 because it is reserved for sign, last bit is 0, because "All" does not include "None"
        All = 2147483646 
    }
}