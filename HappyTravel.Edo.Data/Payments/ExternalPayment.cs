using HappyTravel.Edo.Common.Enums;
using System;

namespace HappyTravel.Edo.Data.Payments
{
    public class ExternalPayment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public int BookingId { get; set; }
        public int? CreditCardId { get; set; }
        public string Currency { get; set; }
        public string AccountNumber { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public PaymentStatuses Status { get; set; }
        public string Data { get; set; }
    }
}
