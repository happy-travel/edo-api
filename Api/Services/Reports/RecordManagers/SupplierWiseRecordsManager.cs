using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports;
using HappyTravel.Edo.Data;

namespace HappyTravel.Edo.Api.Services.Reports.RecordManagers
{
    public class SupplierWiseRecordsManager : IRecordManager<SupplierWiseRecordProjection>
    {
        public SupplierWiseRecordsManager(EdoContext context)
        {
            _context = context;
        }
        
        public IQueryable<SupplierWiseRecordProjection> Get(DateTime fromDate, DateTime endDate)
        {
            return from booking in _context.Bookings
                join invoice in _context.Invoices on booking.ReferenceCode equals invoice.ParentReferenceCode
                join order in _context.SupplierOrders on booking.ReferenceCode equals order.ReferenceCode
                where 
                    booking.IsDirectContract &&
                    booking.Created >= fromDate &&
                    booking.Created < endDate
                select new SupplierWiseRecordProjection
                {
                    ReferenceCode = booking.ReferenceCode,
                    InvoiceNumber = invoice.Number,
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
                    Supplier = booking.Supplier
                };
        }
        
        
        private readonly EdoContext _context;
    }
}