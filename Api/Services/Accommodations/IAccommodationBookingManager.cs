using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IAccommodationBookingManager
    {
        Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest bookingRequest, BookingAvailabilityInfo availabilityInfo, string languageCode);
        Task<Result<AccommodationBookingInfo>> Get(int bookingId);
        Task<Result<AccommodationBookingInfo>> Get(string referenceCode);
        Task<Result<List<SlimAccommodationBookingInfo>>> GetForCurrentCustomer();
        Task<Result<Booking, ProblemDetails>> Cancel(int bookingId);
        Task<Result<BookingDetails, ProblemDetails>> ModifyBooking(int bookingId, BookingModifiedData modifiedData, string languageCode);
    }
}
