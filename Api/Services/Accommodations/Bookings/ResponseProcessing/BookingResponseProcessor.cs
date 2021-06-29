using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.Extensions.Logging;
using Booking = HappyTravel.EdoContracts.Accommodations.Booking;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing
{
    public class BookingResponseProcessor : IBookingResponseProcessor
    {
        public BookingResponseProcessor(IBookingAuditLogService bookingAuditLogService,
            IBookingRecordManager bookingRecordManager,
            ILogger<BookingResponseProcessor> logger,
            IDateTimeProvider dateTimeProvider, 
            EdoContext context,
            IBookingRecordsUpdater recordsUpdater)
        {
            _bookingAuditLogService = bookingAuditLogService;
            _bookingRecordManager = bookingRecordManager;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
            _context = context;
            _recordsUpdater = recordsUpdater;
        }
        
        
        public async Task ProcessResponse(Booking bookingResponse, ApiCaller apiCaller, BookingChangeEvents eventType)
        {
            var (_, isFailure, booking, error) = await _bookingRecordManager.Get(bookingResponse.ReferenceCode);
            if (isFailure)
            {
                _logger.LogBookingResponseProcessFailure(error);
                return;
            }
            
            _logger.LogBookingResponseProcessStarted(bookingResponse.ReferenceCode, booking.Status);

            await _bookingAuditLogService.Add(bookingResponse, booking);

            if (bookingResponse.Status == BookingStatusCodes.NotFound)
            {
                await ProcessBookingNotFound(booking, bookingResponse, eventType);
                return;
            }

            await _recordsUpdater.UpdateWithSupplierData(booking, bookingResponse.SupplierReferenceCode, bookingResponse.BookingUpdateMode,
                bookingResponse.Rooms);
            
            if (bookingResponse.Status.ToInternalStatus() == booking.Status)
            {
                _logger.LogBookingResponseProcessSuccess(bookingResponse.ReferenceCode, "No status changes applied");
                return;
            }

            var changeReason = new BookingChangeReason
            {
                Event = eventType,
                Source = BookingChangeSources.Supplier
            };
            
            var (_, isUpdateFailure, updateError) = await _recordsUpdater.ChangeStatus(booking,
                bookingResponse.Status.ToInternalStatus(),
                _dateTimeProvider.UtcNow(),
                apiCaller, 
                changeReason);
            
            if (isUpdateFailure)
            {
                _logger.LogBookingResponseProcessFailure(updateError);
                return;
            }

            _logger.LogBookingResponseProcessSuccess(bookingResponse.ReferenceCode, $"New status: {bookingResponse.Status}");
        }


        private async Task ProcessBookingNotFound(Data.Bookings.Booking booking, Booking bookingResponse, BookingChangeEvents eventType)
        {
            if (_dateTimeProvider.UtcNow() < booking.Created + BookingCheckTimeout)
            {
                _logger.LogBookingResponseProcessSuccess(bookingResponse.ReferenceCode, $"Has not been processed due to '{BookingStatusCodes.NotFound}' status.");
            }
            else
            {
                await _recordsUpdater.ChangeStatus(booking, BookingStatuses.ManualCorrectionNeeded, _dateTimeProvider.UtcNow(), ApiCaller.InternalServiceAccount, new Data.Bookings.BookingChangeReason 
                { 
                    Source = BookingChangeSources.System,  
                    Event = eventType
                });
                _logger.LogBookingResponseProcessSuccess(bookingResponse.ReferenceCode, "Set as needed manual processing.");
            }
        }

        
        private static readonly TimeSpan BookingCheckTimeout = TimeSpan.FromMinutes(30);
        
        private readonly IBookingAuditLogService _bookingAuditLogService;
        private readonly ILogger<BookingResponseProcessor> _logger;
        private readonly IBookingRecordManager _bookingRecordManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly EdoContext _context;
        private readonly IBookingRecordsUpdater _recordsUpdater;
    }
}