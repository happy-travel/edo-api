using System;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Data.Bookings
{
    public class AppliedBookingMarkup
    {
        public int Id { get; set; }
        public string ReferenceCode { get; set; }
        public int PolicyId { get; set; }
        public decimal Amount { get; set; }
        public Currencies Currency { get; set; }
        public DateTime? Paid { get; set; }
    }
}