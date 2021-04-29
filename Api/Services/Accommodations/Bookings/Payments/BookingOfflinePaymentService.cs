using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Payments.Offline;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public class BookingOfflinePaymentService : IBookingOfflinePaymentService
    {
        public BookingOfflinePaymentService(IBookingRecordManager recordManager,
            IOfflinePaymentAuditService auditService,
            EdoContext context)
        {
            _recordManager = recordManager;
            _auditService = auditService;
            _context = context;
        }
        
        
        public async Task<Result> CompleteOffline(int bookingId, Administrator administratorContext)
        {
            // TODO: Add admin actions audit log NIJO-659
            return await GetBooking()
                .Bind(CheckBookingCanBeCompleted)
                .Tap(Complete)
                .Tap(WriteAuditLog);


            async Task<Result<Booking>> GetBooking()
            {
                var (_, isFailure, booking, _) = await _recordManager.Get(bookingId);
                return isFailure
                    ? Result.Failure<Booking>($"Could not find booking with id {bookingId}")
                    : Result.Success(booking);
            }


            Result<Booking> CheckBookingCanBeCompleted(Booking booking)
                => booking.PaymentStatus == BookingPaymentStatuses.NotPaid
                    ? Result.Success(booking)
                    : Result.Failure<Booking>($"Could not complete booking. Invalid payment status: {booking.PaymentStatus}");


            Task Complete(Booking booking)
            {
                booking.PaymentMethod = PaymentTypes.Offline;
                return ChangeBookingPaymentStatusToCaptured(booking);
            }


            Task WriteAuditLog(Booking booking) => _auditService.Write(administratorContext.ToUserInfo(), booking.ReferenceCode);
            
            Task ChangeBookingPaymentStatusToCaptured(Booking booking)
            {
                booking.PaymentStatus = BookingPaymentStatuses.Captured;
                _context.Bookings.Update(booking);
                return _context.SaveChangesAsync();
            }
        }
        
        
        private readonly IBookingRecordManager _recordManager;
        private readonly IOfflinePaymentAuditService _auditService;
        private readonly EdoContext _context;
    }
}