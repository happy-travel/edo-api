using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports;
using HappyTravel.Edo.Data;

namespace HappyTravel.Edo.Api.Services.Reports.RecordManagers
{
    public class AgencyWiseRecordManager : IRecordManager<AgencyWiseRecordProjection>
    {
        public AgencyWiseRecordManager(EdoContext context)
        {
            _context = context;
        }
        
        
        public IQueryable<AgencyWiseRecordProjection> Get(DateTime fromDate, DateTime endDate)
        {
            return from booking in _context.Bookings
                join invoice in _context.Invoices on booking.ReferenceCode equals invoice.ParentReferenceCode
                join order in _context.SupplierOrders on booking.ReferenceCode equals order.ReferenceCode
                join agency in _context.Agencies on booking.AgencyId equals agency.Id
                where 
                    booking.IsDirectContract &&
                    booking.Created >= fromDate &&
                    booking.Created < endDate
                select new AgencyWiseRecordProjection
                {
                    Date = booking.Created,
                    ReferenceCode = booking.ReferenceCode,
                    InvoiceNumber = invoice.Number,
                    AgencyName = agency.Name,
                    PaymentMethod = booking.PaymentMethod,
                    AccommodationName = booking.AccommodationName,
                    ConfirmationNumber = booking.SupplierReferenceCode,
                    Rooms = booking.Rooms,
                    GuestName = booking.MainPassengerName,
                    ArrivalDate = booking.CheckInDate,
                    DepartureDate = booking.CheckOutDate,
                    OriginalAmount = order.OriginalSupplierPrice,
                    OriginalCurrency = order.OriginalSupplierCurrency,
                    ConvertedAmount = order.ConvertedSupplierPrice,
                    ConvertedCurrency = order.ConvertedSupplierCurrency,
                    PaymentStatus = booking.PaymentStatus
                };
        }
        
        
        private readonly EdoContext _context;
    }
}