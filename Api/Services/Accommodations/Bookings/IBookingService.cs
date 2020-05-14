using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingService
    {
        Task<Result<string, ProblemDetails>> Register(AccommodationBookingRequest bookingRequest, RequestMetadata requestMetadata);

        Task<Result<BookingDetails, ProblemDetails>> Finalize(string referenceCode, RequestMetadata requestMetadata);
        
        Task ProcessResponse(BookingDetails bookingResponse, Data.Booking.Booking booking, RequestMetadata requestMetadata);

        Task<Result<VoidObject, ProblemDetails>> Cancel(int bookingId, RequestMetadata requestMetadata);
        
        Task<Result<BookingDetails, ProblemDetails>> RefreshStatus(int bookingId, RequestMetadata requestMetadata);
    }
}