using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Models.Accommodations;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct  BookingModifiedData
    {
        [JsonConstructor]
        public BookingModifiedData(
            DateTime checkInDate,
            DateTime checkOutDate,
            List<BookingRoomDetails> roomDetails,
            List<AccommodationFeature> features,
            string nationality,
            string residency,
            bool rejectIfUnavailable)
        {
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            RoomDetails = roomDetails;
            Features = features;
            Nationality = nationality;
            Residency = residency;
            RejectIfUnavailable = rejectIfUnavailable;
        }

        
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public List<BookingRoomDetails> RoomDetails { get; }
        public List<AccommodationFeature> Features { get; }
        public string Nationality { get; }
        public string Residency { get; }
        public bool RejectIfUnavailable { get; }
    }
}
