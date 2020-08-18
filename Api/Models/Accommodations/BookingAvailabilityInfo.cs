using System;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Geography;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct BookingAvailabilityInfo
    {
        [JsonConstructor]
        public BookingAvailabilityInfo(
            string accommodationId,
            string accommodationName,
            in RoomContractSet roomContractSet,
            string zoneCode,
            string zoneName,
            string localityCode,
            string localityName,
            string countryCode,
            string countryName,
            string address,
            GeoPoint coordinates,
            DateTime checkInDate,
            DateTime checkOutDate,
            int numberOfNights,
            DataProviders dataProvider)
        {
            AccommodationId = accommodationId;
            AccommodationName = accommodationName;
            RoomContractSet = roomContractSet;
            ZoneCode = zoneCode;
            ZoneName = zoneName;
            LocalityCode = localityCode;
            LocalityName = localityName;
            CountryCode = countryCode;
            CountryName = countryName;
            Address = address;
            Coordinates = coordinates;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            NumberOfNights = numberOfNights;
            DataProvider = dataProvider;
        }


        public string AccommodationId { get; }
        public string AccommodationName { get; }
        public RoomContractSet RoomContractSet { get; }
        public string ZoneCode { get; }
        public string ZoneName { get; }
        public string LocalityCode { get; }
        public string LocalityName { get; }
        public string CountryCode { get; }
        public string CountryName { get; }
        public string Address { get; }
        public GeoPoint Coordinates { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public int NumberOfNights { get; }
        public DataProviders DataProvider { get; }


        public bool Equals(BookingAvailabilityInfo other)
            => (AccommodationId, AccommodationName, RoomContractSet: RoomContractSet, CityCode: LocalityCode, LocalityName, CountryCode, CountryName, CheckInDate, CheckOutDate, NumberOfNights)
                .Equals((other.AccommodationId, other.AccommodationName, other.RoomContractSet, other.LocalityCode, other.LocalityName,
                    other.CountryCode, other.CountryName, other.CheckInDate, other.CheckOutDate, NumberOfNights));


        public override bool Equals(object obj) => obj is BookingAvailabilityInfo other && Equals(other);


        public override int GetHashCode()
            => (AccommodationId, AccommodationName, RoomContractSet: RoomContractSet, CityCode: LocalityCode, LocalityName, CountryCode, CountryName, CheckInDate, CheckOutDate)
                .GetHashCode();
    }
}