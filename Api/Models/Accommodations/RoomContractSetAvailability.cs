using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct RoomContractSetAvailability
    {
        [JsonConstructor]
        public RoomContractSetAvailability(string availabilityId, DateTime checkInDate, DateTime checkOutDate, int numberOfNights,
            in SlimAccommodation accommodation, in RoomContractSet roomContractSet, List<PaymentTypes> availablePaymentMethods)
        {
            AvailabilityId = availabilityId;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            NumberOfNights = numberOfNights;
            Accommodation = accommodation;
            RoomContractSet = roomContractSet;
            AvailablePaymentMethods = availablePaymentMethods;
        }
        
        /// <summary>
        ///     The availability ID.
        /// </summary>
        public string AvailabilityId { get; }

        /// <summary>
        ///     The check-in date.
        /// </summary>
        public DateTime CheckInDate { get; }

        /// <summary>
        ///     The check-out date.
        /// </summary>
        public DateTime CheckOutDate { get; }

        /// <summary>
        ///     The number of nights to stay.
        /// </summary>
        public int NumberOfNights { get; }

        /// <summary>
        ///     Information about a selected accommodation.
        /// </summary>
        public SlimAccommodation Accommodation { get; }

        /// <summary>
        ///     Information about a selected room contract set.
        /// </summary>
        public RoomContractSet RoomContractSet { get; }

        /// <summary>
        /// List of available payment methods
        /// </summary>
        public List<PaymentTypes> AvailablePaymentMethods { get; }
    }
}