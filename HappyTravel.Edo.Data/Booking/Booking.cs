using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Booking
{
    public class Booking : IEntity
    {
        public int Id { get; set; }
        
        public int CustomerId { get; set; }
        public int CompanyId { get; set; }
        public DateTime Created { get; set; }
        
        public string AgentReference { get; set; }
        public string ReferenceCode { get; set; }
        
        public BookingStatusCodes Status { get; set; }
        public BookingPaymentStatuses PaymentStatus { get; set; }
        public DateTime BookingDate { get; set; }
        public string Nationality { get; set; }
        public string Residency { get; set; }
        public string ItineraryNumber { get; set; }
        public string MainPassengerName { get; set; }
        public ServiceTypes ServiceType { get; set; }
        public PaymentMethods PaymentMethod { get; set; }
        
        public string BookingDetails { get; set; }
        public string ServiceDetails { get; set; }
    }
}