using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public struct SlimAccommodationBookingInfo
    {
        public SlimAccommodationBookingInfo(Booking bookingInfo)
        {
            var serviceDetails = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(bookingInfo.ServiceDetails);
            var bookingDetails = JsonConvert.DeserializeObject<BookingDetails>(bookingInfo.BookingDetails);
            
            //TODO change with the contracts update up to 1.3.0 
            var firstRoomContract = serviceDetails.Agreement.RoomContracts.First();
            
            Id = bookingInfo.Id;
            ReferenceCode = bookingDetails.ReferenceCode;
            AccommodationName = serviceDetails.AccommodationName;
            CountryName = serviceDetails.CountryName;
            LocalityName = serviceDetails.CityName;
            Deadline = bookingDetails.Deadline;
            DeadlineDetails = serviceDetails.DeadlineDetails;
            BoardBasisCode = firstRoomContract.BoardBasisCode;
            BoardBasis = firstRoomContract.BoardBasis;
            Price = serviceDetails.Agreement.Price;
            CheckInDate = bookingDetails.CheckInDate;
            CheckOutDate = bookingDetails.CheckOutDate;
            Status = bookingDetails.Status;
            MealPlan = firstRoomContract.MealPlan;
            MealPlanCode = firstRoomContract.MealPlanCode;
            ContractType = firstRoomContract.ContractType;
            PaymentStatus = bookingInfo.PaymentStatus;
        }


        public int Id { get; }

        public string ReferenceCode { get; }

        public BookingStatusCodes Status { get; }

        public Price Price { get; }

        public string BoardBasisCode { get; }

        public string BoardBasis { get; }

        public DateTime CheckOutDate { get; }

        public DateTime CheckInDate { get; }

        public string LocalityName { get; }

        public string CountryName { get; }

        public string AccommodationName { get; }

        public DateTime Deadline { get; }

        public DeadlineDetails DeadlineDetails { get; }

        public string MealPlan { get; }

        public string MealPlanCode { get; }

        public string ContractType { get; }

        public BookingPaymentStatuses PaymentStatus { get; }
    }
}