using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IAccommodationService
    {
        ValueTask<Result<RichAccommodationDetails, ProblemDetails>> Get(string accommodationId, string languageCode);
        Task<Result<AccommodationBookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request, string languageCode);
        Task<List<AccommodationBookingInfo>> GetBookings();
        Task<Result<VoidObject, ProblemDetails>> CancelBooking(int bookingId);
    }
}