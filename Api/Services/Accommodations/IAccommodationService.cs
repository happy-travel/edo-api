using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IAccommodationService
    {
        ValueTask<Result<AccommodationDetails, ProblemDetails>> Get(string accommodationId, string languageCode);
        ValueTask<Result<AvailabilityDetails, ProblemDetails>> GetAvailable(Models.Availabilities.AvailabilityRequest request, string languageCode);
        Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request, string languageCode);
        Task<Result<AccommodationBookingInfo>> GetBooking(int bookingId);
        Task<Result<AccommodationBookingInfo>> GetBooking(string referenceCode);
        Task<Result<List<SlimAccommodationBookingInfo>>> GetCustomerBookings();
        Task<Result<VoidObject, ProblemDetails>> CancelBooking(int bookingId);
        Task<Result<BookingAvailabilityInfo, ProblemDetails>> GetBookingAvailability(int availabilityId, Guid agreementId, string languageCode);
        Task<Result<ListOfBookingIds>> GetBookingsForCancellation(DateTime deadlineDate);
        Task<Result<string>> CancelBookings(ListOfBookingIds model);
    }
}