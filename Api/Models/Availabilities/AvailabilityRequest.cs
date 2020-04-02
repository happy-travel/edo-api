using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Availabilities
{
    public readonly struct AvailabilityRequest
    {
        [JsonConstructor]
        public AvailabilityRequest(string nationality, string residency, DateTime checkInDate, DateTime checkOutDate,
            SearchFilters filters, List<RoomRequestDetails> roomDetails, AccommodationRatings ratings,
            SearchLocation location = default, PropertyTypes propertyTypes = default, SearchInfo searchInfo = default)
        {
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            Filters = filters;
            Location = location;
            Nationality = nationality;
            PropertyType = propertyTypes;
            Ratings = ratings;
            Residency = residency;
            RoomDetails = roomDetails ?? new List<RoomRequestDetails>();
            SearchInfo = searchInfo;
        }


        /// <summary>
        ///     Required. Check-in date.
        /// </summary>
        [Required]
        public DateTime CheckInDate { get; }

        /// <summary>
        ///     Required. Check-out date.
        /// </summary>
        [Required]
        public DateTime CheckOutDate { get; }

        /// <summary>
        ///     One ore several filters to order a response data.
        /// </summary>
        public SearchFilters Filters { get; }

        /// <summary>
        ///     Desirable search area.
        /// </summary>
        public SearchLocation Location { get; }

        /// <summary>
        ///     Required. Alpha-2 nationality code for a lead passengers.
        /// </summary>
        [Required]
        public string Nationality { get; }

        /// <summary>
        ///     Desirable property type for an accommodation.
        /// </summary>
        public PropertyTypes PropertyType { get; }

        /// <summary>
        ///     Accommodation rating.
        /// </summary>
        public AccommodationRatings Ratings { get; }

        /// <summary>
        ///     Required. Alpha-2 residency code for a lead passengers.
        /// </summary>
        [Required]
        public string Residency { get; }

        /// <summary>
        ///     Required. Desirable room details.
        /// </summary>
        [Required]
        public List<RoomRequestDetails> RoomDetails { get; }

        public SearchInfo SearchInfo { get; }
    }
}