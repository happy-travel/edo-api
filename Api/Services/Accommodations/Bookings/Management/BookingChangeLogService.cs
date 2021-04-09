using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public class BookingChangeLogService : IBookingChangeLogService
    {
        public BookingChangeLogService(EdoContext context)
        {
            _context = context;
        }


        public async Task Write(Booking booking, BookingStatuses status, DateTime date, ApiCaller apiCaller, BookingChangeReason reason)
        {
            var bookingStatusHistoryEntry = new BookingStatusHistoryEntry
            {
                BookingId = booking.Id,
                UserId = apiCaller.Id,
                ApiCallerType = apiCaller.Type,
                CreatedAt = date,
                Status = status,
                Initiator = GetInitiatorType(apiCaller),
                Source = reason.Source,
                Event = reason.Event,
                Reason = reason.Reason
            };
            
            if (apiCaller.Type == ApiCallerTypes.Agent)
                bookingStatusHistoryEntry.AgencyId = booking.AgencyId;

            var entry = _context.BookingStatusHistory.Add(bookingStatusHistoryEntry);
            await _context.SaveChangesAsync();
            _context.Detach(entry.Entity);


            static BookingChangeInitiators GetInitiatorType(ApiCaller userInfo)
                => userInfo.Type switch
                {
                    ApiCallerTypes.Admin => BookingChangeInitiators.Administrator,
                    ApiCallerTypes.Agent => BookingChangeInitiators.Agent,
                    ApiCallerTypes.ServiceAccount => BookingChangeInitiators.System,
                    ApiCallerTypes.InternalServiceAccount => BookingChangeInitiators.System,
                    ApiCallerTypes.Supplier => BookingChangeInitiators.Supplier,
                    _ => throw new ArgumentOutOfRangeException()
                };
        }
        
        
        private readonly EdoContext _context;
    }
}