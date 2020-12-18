using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingPaymentService : IPaymentsService
    {
        Task<Result<string>> Capture(Booking booking, UserInfo user);

        Task<Result<string>> Charge(Booking booking, UserInfo user);

        Task<Result> VoidOrRefund(Booking booking, UserInfo user);

        Task<Result> CompleteOffline(int bookingId, Administrator administratorContext);
    }
}