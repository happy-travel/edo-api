using System;

namespace HappyTravel.Edo.Data.Bookings
{
    public class BookingAuditLogEntry
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public int AgentId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BookingDetails {get; set; }
    }
}