using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct AccommodationBookingDetails
    {
        [JsonConstructor]
        public AccommodationBookingDetails(string referenceCode, string agentReference, BookingStatuses status, int numberOfNights,
            DateTime checkInDate, DateTime checkOutDate, AccommodationLocation location, ContactInfo contactInfo,
            string accommodationId, string accommodationName, DateTime? deadlineDate,
            List<BookedRoom> roomDetails, int numberOfPassengers)
        {
            ReferenceCode = referenceCode;
            AgentReference = agentReference;
            Status = status;
            NumberOfNights = numberOfNights;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            Location = location;
            ContactInfo = contactInfo;
            AccommodationId = accommodationId;
            AccommodationName = accommodationName;
            DeadlineDate = deadlineDate;
            NumberOfPassengers = numberOfPassengers;
            RoomDetails = roomDetails ?? new List<BookedRoom>(0);
        }
        

        public override bool Equals(object obj) => obj is AccommodationBookingDetails other && Equals(other);


        public bool Equals(AccommodationBookingDetails other)
            => (ReferenceCode, Status, CheckInDate, CheckOutDate, AccommodationId, AccommodationName, DeadlineDate, RoomDetails, NumberOfPassengers) ==
                (other.ReferenceCode, other.Status, other.CheckInDate, other.CheckOutDate, other.AccommodationId, other.AccommodationName,
                    other.DeadlineDate, other.RoomDetails, other.NumberOfPassengers);


        public override int GetHashCode()
            => (ReferenceCode, Status, CheckInDate, CheckOutDate, LocationInfo: Location, AccommodationId, AccommodationName, DeadlineDate, RoomDetails).GetHashCode();


        public string ReferenceCode { get; }
        public string AgentReference { get; }
        public BookingStatuses Status { get; }
        public int NumberOfNights { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public string AccommodationId { get; }
        public string AccommodationName { get; }
        public AccommodationLocation Location { get; }
        public ContactInfo ContactInfo { get; }
        public DateTime? DeadlineDate { get; }
        public int NumberOfPassengers { get; }
        public List<BookedRoom> RoomDetails { get; }
    }
}