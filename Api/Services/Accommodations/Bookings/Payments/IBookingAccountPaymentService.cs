using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public interface IBookingAccountPaymentService
    {
        Task<Result<string>> Charge(Booking booking, ApiCaller apiCaller);

        Task<Result> Refund(Booking booking, DateTime operationDate, ApiCaller apiCaller);
    }
}