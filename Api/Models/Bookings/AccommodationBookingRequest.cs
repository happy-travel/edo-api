using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.EdoContracts.General.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct AccommodationBookingRequest
    {
        [JsonConstructor]
        public AccommodationBookingRequest(string itineraryNumber, string nationality, PaymentMethods paymentMethod, string residency, 
            List<BookingRoomDetails> roomDetails, List<AccommodationFeature> features,
            Guid searchId,
            Guid resultId,
            Guid roomContractSetId,
            string mainPassengerName,
            bool rejectIfUnavailable = true,
            string availabilityId = null)
        {
            ItineraryNumber = itineraryNumber;
            Nationality = nationality;
            RejectIfUnavailable = rejectIfUnavailable;
            AvailabilityId = availabilityId;
            Residency = residency;
            SearchId = searchId;
            ResultId = resultId;
            RoomContractSetId = roomContractSetId;
            MainPassengerName = mainPassengerName;
            PaymentMethod = paymentMethod;

            RoomDetails = roomDetails ?? new List<BookingRoomDetails>(0);
            Features = features ?? new List<AccommodationFeature>(0);
        }


        public AccommodationBookingRequest(AccommodationBookingRequest request, string availabilityId) :
            this(request.ItineraryNumber, request.Nationality, request.PaymentMethod,
                request.Residency, request.RoomDetails, request.Features, request.SearchId, request.ResultId, request.RoomContractSetId,
                request.MainPassengerName,
                request.RejectIfUnavailable, availabilityId)
        {
            
        }


        /// <summary>
        ///     The nationality of a main passenger.
        /// </summary>
        [Required]
        public string Nationality { get; }

        /// <summary>
        ///     This indicates the system to reject the request when an accommodation has been booked by some one else between
        ///     availability and booking requests. Default is true.
        /// </summary>
        public bool RejectIfUnavailable { get; }

        public string AvailabilityId { get; }

        /// <summary>
        ///     The residency of a main passenger.
        /// </summary>
        [Required]
        public string Residency { get; }

        public Guid SearchId { get; }
        public Guid ResultId { get; }

        /// <summary>
        ///     Room details from an availability response.
        /// </summary>
        public List<BookingRoomDetails> RoomDetails { get; }

        /// <summary>
        ///     The selected additional accommodation features, if any.
        /// </summary>
        public List<AccommodationFeature> Features { get; }

        /// <summary>
        ///     Identifier of chosen room contract set.
        /// </summary>
        [Required]
        public Guid RoomContractSetId { get; }

        /// <summary>
        ///     The full name of main passenger (buyer).
        /// </summary>
        [Required]
        public string MainPassengerName { get; }

        /// <summary>
        ///     Itinerary number to combine several orders in one pack.
        /// </summary>
        public string ItineraryNumber { get; }

        /// <summary>
        ///     Payment method for a booking.
        /// </summary>
        [Required]
        public PaymentMethods PaymentMethod { get; }
    }
}