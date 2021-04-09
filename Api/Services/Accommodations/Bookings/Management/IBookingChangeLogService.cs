using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public interface IBookingChangeLogService
    {
        Task Write(Booking booking, BookingStatuses status, DateTime date, ApiCaller apiCaller, BookingChangeReason reason);
    }
}