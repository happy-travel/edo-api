using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public interface IBookingCreditCardPaymentService
    {
        Task<Result<string>> Capture(Booking booking, ApiCaller apiCaller);

        Task<Result> Void(Booking booking, ApiCaller apiCaller);

        Task<Result> Refund(Booking booking, DateTime operationDate, ApiCaller apiCaller);

        Task<Result> PayForAccountBooking(string referenceCode, AgentContext agent);
    }
}