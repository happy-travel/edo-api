using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public interface IBookingMoneyReturnService
    {
        Task<Result> ReturnMoney(Booking booking, DateTime operationDate, ApiCaller apiCaller);
    }
}