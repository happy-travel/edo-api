using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Money.Models;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct SlimAccommodationBookingInfo
    {
        [JsonConstructor]
        public SlimAccommodationBookingInfo(Booking bookingInfo)
        {
            Id = bookingInfo.Id;
            ReferenceCode = bookingInfo.ReferenceCode;
            AccommodationName = bookingInfo.AccommodationName;
            CountryName = bookingInfo.Location.Country;
            LocalityName = bookingInfo.Location.Locality;
            Deadline = bookingInfo.DeadlineDate;
            Price = new MoneyAmount(bookingInfo.TotalPrice, bookingInfo.Currency);
            CheckInDate = bookingInfo.CheckInDate;
            CheckOutDate = bookingInfo.CheckOutDate;
            Status = bookingInfo.Status;
            PaymentStatus = bookingInfo.PaymentStatus;
            Rooms = bookingInfo.Rooms;
            Supplier = bookingInfo.Supplier;
        }

        public int Id { get; init; }

        public string ReferenceCode { get; init; }

        public BookingStatuses Status { get; init; }

        public MoneyAmount Price { get; init; }

        public DateTime CheckOutDate { get; init; }

        public DateTime CheckInDate { get; init; }

        public string LocalityName { get; init; }

        public string CountryName { get; init; }

        public string AccommodationName { get; init; }

        public DateTime? Deadline { get; init; }

        public BookingPaymentStatuses PaymentStatus { get; init; }

        public List<BookedRoom> Rooms { get; init; }
        
        public Suppliers? Supplier { get; init; }
    }
}