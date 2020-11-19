using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data.Management;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingCreditCardPaymentConfirmationService
    {
        Task<Result<Data.Booking.Booking, ProblemDetails>> Confirm(int bookingId, Administrator administrator);
    }
}