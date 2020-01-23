using HappyTravel.Edo.Common.Enums;
using System;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Data.Payments
{
    public class Payment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public int BookingId { get; set; }
        public int? AccountId { get; set; }
        public string Currency { get; set; }
        public string AccountNumber { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public PaymentStatuses Status { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        public string Data { get; set; }
    }
}
