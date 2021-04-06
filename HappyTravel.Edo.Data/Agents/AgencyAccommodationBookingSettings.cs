using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums.AgencySettings;

namespace HappyTravel.Edo.Data.Agents
{
    public class AgencyAccommodationBookingSettings
    {
        public bool IsMarkupDisabled { get; set; }
        
        public AprMode? AprMode { get; set; }
        
        public PassedDeadlineOffersMode? PassedDeadlineOffersMode { get; set; }
        
        public List<Common.Enums.Suppliers> EnabledSuppliers { get; set; }
        
        public bool IsSupplierVisible { get; set; }
        
        public bool IsDirectContractFlagVisible { get; set; }
        
        public int? CustomDeadlineShift { get; set; }
    }
}