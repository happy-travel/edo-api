using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.General.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct AccommodationBookingRequest
    {
        [JsonConstructor]
        public AccommodationBookingRequest(string accommodationId, string availabilityId, DateTime checkInDate, DateTime checkOutDate,
            string itineraryNumber, string nationality, PaymentMethods paymentMethod, string residency, string tariffCode,
            List<BookingRoomDetails> roomDetails, List<AccommodationFeature> features, string agentReference,
            Guid agreementId,
            string mainPassengerName,
            string mainPassengerFirstName,
            DataProviders dataProvider,
            string countryCode = default,
            bool rejectIfUnavailable = true)
        {
            AvailabilityId = availabilityId;
            ItineraryNumber = itineraryNumber;
            Nationality = nationality;
            RejectIfUnavailable = rejectIfUnavailable;
            Residency = residency;
            AgreementId = agreementId;
            MainPassengerName = mainPassengerName;
            PaymentMethod = paymentMethod;

            RoomDetails = roomDetails ?? new List<BookingRoomDetails>(0);
            Features = features ?? new List<AccommodationFeature>(0);

            DataProvider = dataProvider;
        }


        /// <summary>
        ///     Availability ID obtained from an <see cref="AvailabilityDetails" />.
        /// </summary>
        [Required]
        public string AvailabilityId { get; }

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

        /// <summary>
        ///     The residency of a main passenger.
        /// </summary>
        [Required]
        public string Residency { get; }

        /// <summary>
        ///     Room details from an availability response.
        /// </summary>
        public List<BookingRoomDetails> RoomDetails { get; }

        /// <summary>
        ///     The selected additional accommodation features, if any.
        /// </summary>
        public List<AccommodationFeature> Features { get; }

        /// <summary>
        ///     Identifier of chosen agreement.
        /// </summary>
        [Required]
        public Guid AgreementId { get; }

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

        /// <summary>
        ///     Accommodation source from search results
        /// </summary>
        [Required]
        public DataProviders DataProvider { get; }
    }
}