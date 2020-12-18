using System.Threading.Tasks;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Booking = HappyTravel.EdoContracts.Accommodations.Booking;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingAuditLogService: IBookingAuditLogService
    {
        public BookingAuditLogService(EdoContext edoContext)
        {
            _edoContext = edoContext;
        }
        
        /// <summary>
        /// Adds new booking details data to the log table
        /// </summary>
        /// <param name="newBookingDetails">Booking details that will be written to the booking table</param>
        /// <param name="currentBookingData">Current booking data from the booking table where BookingDetails value will be rewritten by the newBookingDetails value</param>
        /// <returns></returns>
        public async Task Add(Booking newBookingDetails, Data.Bookings.Booking currentBookingData = null)
        {
            if (currentBookingData is null)
                currentBookingData = await _edoContext.Bookings.SingleOrDefaultAsync(i => i.ReferenceCode.Equals(newBookingDetails.ReferenceCode));

            await Add(currentBookingData.Id,
                currentBookingData.AgentId,
                newBookingDetails);
        }


        private Task Add(int bookingId, int agentId,  Booking newBookingDetails)
        {
            _edoContext.BookingAuditLog.Add(new BookingAuditLogEntry
            {
                BookingId = bookingId,
                AgentId = agentId,
                BookingDetails = JsonConvert.SerializeObject(newBookingDetails),
            });
            
            return _edoContext.SaveChangesAsync();
        }
        
        
        private readonly EdoContext _edoContext;
    }
}