using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingChangesProcessor
    {
        Task<Result> ProcessCancellation(Booking booking, UserInfo user);

        Task ProcessConfirmation(Booking booking, EdoContracts.Accommodations.Booking bookingResponse);

        Task ProcessBookingNotFound(Booking booking, EdoContracts.Accommodations.Booking bookingResponse);
    }
}