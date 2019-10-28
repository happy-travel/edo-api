using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Payments
{
    public class CreditCard
    {
        public int Id { get; set; }
        public string ReferenceCode { get; set; }
        public string MaskedNumber { get; set; }
        public string ExpirationDate { get; set; }
        public string Token { get; set; }
        public string HolderName { get; set; }
        public bool Used { get; set; }
        public CreditCardOwnerType OwnerType { get; set; }
        public int OwnerId { get; set; }
    }
}
