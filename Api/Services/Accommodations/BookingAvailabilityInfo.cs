using System;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public readonly struct BookingAvailabilityInfo
    {
        [JsonConstructor]
        public BookingAvailabilityInfo(
            string accommodationId,
            string accommodationName,
            in RichAgreement agreement,
            string cityCode,
            string cityName,
            string countryCode,
            string countryName,
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            AccommodationId = accommodationId;
            AccommodationName = accommodationName;
            Agreement = agreement;
            CityCode = cityCode;
            CityName = cityName;
            CountryCode = countryCode;
            CountryName = countryName;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            DeadlineDetails = default;
        }
        private BookingAvailabilityInfo(
            string accommodationId,
            string accommodationName,
            in RichAgreement agreement,
            DeadlineDetails deadlineDetails,
            string cityCode,
            string cityName,
            string countryCode,
            string countryName,
            DateTime checkInDate,
            DateTime checkOutDate)
        {
            AccommodationId = accommodationId;
            AccommodationName = accommodationName;
            Agreement = agreement;
            DeadlineDetails = deadlineDetails;
            CityCode = cityCode;
            CityName = cityName;
            CountryCode = countryCode;
            CountryName = countryName;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
        }

        public BookingAvailabilityInfo AddDeadlineDetails(DeadlineDetails deadlineDetails)
        {
            return new BookingAvailabilityInfo(AccommodationId, AccommodationName, Agreement, deadlineDetails, CityCode, CityName, CountryCode, CountryName, CheckInDate, CheckOutDate);
        }
        
        public string AccommodationId { get; }
        public string AccommodationName { get; }
        public RichAgreement Agreement { get; }
        public DeadlineDetails DeadlineDetails { get; }
        public string CityCode { get; }
        public string CityName { get; }
        public string CountryCode { get; }
        public string CountryName { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }

        public bool Equals(BookingAvailabilityInfo other)
        {
            return (AccommodationId, AccommodationName, Agreement, DeadlineDetails, CityCode, CityName, CountryCode, CountryName, CheckInDate, CheckOutDate)
                .Equals((other.AccommodationId, other.AccommodationName, other.Agreement, other.DeadlineDetails, other.CityCode, other.CityName, other.CountryCode, other.CountryName, other.CheckInDate, other.CheckOutDate));
        }

        public override bool Equals(object obj)
        {
            return obj is BookingAvailabilityInfo other && Equals(other);
        }

        public override int GetHashCode()
            => (AccommodationId, AccommodationName, Agreement, DeadlineDetails, CityCode, CityName, CountryCode, CountryName, CheckInDate, CheckOutDate).GetHashCode();
    }
}