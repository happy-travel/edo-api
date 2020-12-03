using System;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Data.Markup
{
    public class PaymentMarkupLog
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int BookingId { get; set; }
        public Currencies Currency { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}