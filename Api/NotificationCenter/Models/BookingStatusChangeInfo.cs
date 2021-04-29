using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using System;

namespace HappyTravel.Edo.Api.NotificationCenter.Models
{
    public readonly struct BookingStatusChangeInfo
    {
        public int BookingId { get; init; }
        public string ReferenceCode { get; init; }
        public BookingStatuses Status { get; init; }
        public DateTime ChangeTime { get; init; }
        public string AccommodationName { get; init; }
        public ImageInfo AccommodationPhoto { get; init; }
        public DateTime CheckInDate { get; init; }
        public DateTime CheckOutDate { get; init; }
    }
}
