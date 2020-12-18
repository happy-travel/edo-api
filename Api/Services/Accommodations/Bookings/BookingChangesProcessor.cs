using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingChangesProcessor : IBookingChangesProcessor
    {
        public BookingChangesProcessor(ISupplierOrderService supplierOrderService,
            IBookingRecordsManager bookingRecordsManager,
            IBookingPaymentService paymentService,
            IBookingMailingService bookingMailingService,
            ILogger<BookingChangesProcessor> logger,
            IDateTimeProvider dateTimeProvider,
            EdoContext context)
        {
            _supplierOrderService = supplierOrderService;
            _bookingRecordsManager = bookingRecordsManager;
            _paymentService = paymentService;
            _bookingMailingService = bookingMailingService;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
            _context = context;
        }
        
        public Task<Result> ProcessCancellation(Booking booking, UserInfo user)
        {
            return SendNotifications()
                .Tap(CancelSupplierOrder)
                .Bind(VoidMoney)
                .Tap(SetBookingCancelled);

            
            Task CancelSupplierOrder() => _supplierOrderService.Cancel(booking.ReferenceCode);


            async Task<Result> SendNotifications()
            {
                var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == booking.AgentId);
                if (agent == default)
                {
                    _logger.LogWarning("Booking cancellation notification: could not find agent with id '{0}' for the booking '{1}'",
                        booking.AgentId, booking.ReferenceCode);

                    return Result.Success();
                }

                var (_, _, bookingInfo, _) = await _bookingRecordsManager.GetAccommodationBookingInfo(booking.ReferenceCode, booking.LanguageCode);
                await _bookingMailingService.NotifyBookingCancelled(bookingInfo);
                
                return Result.Success();
            }

            async Task<Result> VoidMoney()
            {
                if (booking.PaymentStatus == BookingPaymentStatuses.Authorized || booking.PaymentStatus == BookingPaymentStatuses.Captured)
                {
                    var (_, isFailure, error) = await _paymentService.VoidOrRefund(booking, user);
                    if (isFailure)
                        return Result.Failure(error);

                    switch (booking.PaymentStatus)
                    {
                        case BookingPaymentStatuses.Authorized:
                            await _bookingRecordsManager.SetPaymentStatus(booking, BookingPaymentStatuses.Voided);
                            break;
                        case BookingPaymentStatuses.Captured:
                            await _bookingRecordsManager.SetPaymentStatus(booking, BookingPaymentStatuses.Refunded);
                            break;
                    }
                }

                return Result.Success();
            }


            Task SetBookingCancelled() => _bookingRecordsManager.SetStatus(booking, BookingStatuses.Cancelled);
        }
        
        
        public async Task ProcessConfirmation(Booking booking, EdoContracts.Accommodations.Booking bookingResponse)
        {
            await GetBookingInfo(booking.ReferenceCode, booking.LanguageCode)
                .Tap(Confirm)
                .Tap(NotifyBookingFinalization)
                .Bind(SendInvoice)
                .OnFailure(WriteFailureLog);
            
            Task<Result<AccommodationBookingInfo>> GetBookingInfo(string referenceCode, string languageCode) => _bookingRecordsManager
                .GetAccommodationBookingInfo(referenceCode, languageCode);


            Task Confirm(AccommodationBookingInfo bookingInfo) => _bookingRecordsManager.Confirm(bookingResponse, booking);
            
            
            Task NotifyBookingFinalization(AccommodationBookingInfo bookingInfo) => _bookingMailingService
                .NotifyBookingFinalized(bookingInfo);


            Task<Result> SendInvoice(AccommodationBookingInfo bookingInfo) => _bookingMailingService
                .SendInvoice(bookingInfo.BookingId, bookingInfo.AgentInformation.AgentEmail, booking.AgentId);


            void WriteFailureLog(string error) => _logger
                .LogBookingConfirmationFailure($"Booking '{booking.ReferenceCode} confirmation failed: '{error}");
        }
        
        
        public async Task ProcessBookingNotFound(Booking booking, EdoContracts.Accommodations.Booking bookingResponse)
        {
            if (_dateTimeProvider.UtcNow() < booking.Created + BookingCheckTimeout)
            {
                _logger.LogBookingResponseProcessSuccess(
                    $"The booking response with the reference code '{bookingResponse.ReferenceCode}' has not been processed due to '{BookingStatusCodes.NotFound}' status.");
            }
            else
            {
                await _bookingRecordsManager.SetStatus(booking, BookingStatuses.ManualCorrectionNeeded);
                _logger.LogBookingResponseProcessSuccess(
                    $"The booking response with the reference code '{bookingResponse.ReferenceCode}' set as needed manual processing.");
            }
        }
        
        private static readonly TimeSpan BookingCheckTimeout = TimeSpan.FromMinutes(30);
        
        private readonly ISupplierOrderService _supplierOrderService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingPaymentService _paymentService;
        private readonly IBookingMailingService _bookingMailingService;
        private readonly ILogger<BookingChangesProcessor> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly EdoContext _context;
    }
}