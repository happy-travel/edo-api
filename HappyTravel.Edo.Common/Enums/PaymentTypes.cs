using System.ComponentModel;

namespace HappyTravel.Edo.Common.Enums
{
    public enum PaymentTypes
    {
        [Description("Other")] 
        None = 0,
        
        [Description("Virtual Account")] 
        VirtualAccount = 1,
        
        [Description("Credit Card")] 
        CreditCard = 2,
        
        [Description("Offline")] 
        Offline = 3,
    }
}