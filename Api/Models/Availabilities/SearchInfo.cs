﻿using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Availabilities
{
    public readonly struct SearchInfo
    {
        [JsonConstructor]
        public SearchInfo(string availabilityId, string tariffCode, decimal price = decimal.Zero, string hotelId = null)
        {
            AvailabilityId = availabilityId;
            HotelId = hotelId;
            Price = price;
            TariffCode = tariffCode;
        }


        public SearchInfo(string availabilityId, string tariffCode, string hotelId) : this(availabilityId, tariffCode, 0, hotelId)
        { }


        /// <summary>
        ///     Search number obtained from a previous search.
        /// </summary>
        public string AvailabilityId { get; }

        /// <summary>
        ///     Netstorming ID of a desirable hotel.
        /// </summary>
        public string HotelId { get; }

        /// <summary>
        ///     Room price obtained from a previous search.
        /// </summary>
        public decimal Price { get; }

        /// <summary>
        ///     Code of the roomContactSet obtained from a previous search.
        /// </summary>
        public string TariffCode { get; }
    }
}