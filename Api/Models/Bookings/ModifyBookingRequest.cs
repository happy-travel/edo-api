using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct ModifyBookingRequest
    {
        [JsonConstructor]
        public ModifyBookingRequest(DateTime checkInDate, 
            DateTime checkOutDate, 
            List<RoomDetails> roomDetails, 
            List<AccommodationFeature> features,
            BookingRequest bookingRequest,
            SearchInfo searchInfo)
        {
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            RoomDetails = roomDetails;
            Features = features;
            BookingRequest = bookingRequest;
            SearchInfo = searchInfo;
        }


        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public List<RoomDetails> RoomDetails { get; }
        public List<AccommodationFeature> Features { get; }
        public BookingRequest BookingRequest { get; }
        public SearchInfo SearchInfo { get; }
    }
}
