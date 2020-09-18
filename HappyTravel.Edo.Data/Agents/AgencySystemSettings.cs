using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Agents
{
    public class AgencySystemSettings
    {
        public int AgencyId { get; set; }
        
        public AgencyAvailabilitySearchSettings AvailabilitySearchSettings { get; set; }

        public DisplayedPaymentOptionsSettings? DisplayedPaymentOptions { get; set; }
    }
}