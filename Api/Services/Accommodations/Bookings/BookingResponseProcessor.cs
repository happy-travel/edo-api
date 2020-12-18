using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingResponseProcessor : IBookingResponseProcessor
    {
        public BookingResponseProcessor(IBookingAuditLogService bookingAuditLogService,
            IBookingRecordsManager bookingRecordsManager,
            IBookingChangesProcessor bookingChangesProcessor,
            ILogger<BookingResponseProcessor> logger)
        {
            _bookingAuditLogService = bookingAuditLogService;
            _bookingRecordsManager = bookingRecordsManager;
            _bookingChangesProcessor = bookingChangesProcessor;
            _logger = logger;
        }
        
        public async Task ProcessResponse(Booking bookingResponse, Data.Bookings.Booking booking)
        {
            await _bookingAuditLogService.Add(bookingResponse, booking);

            _logger.LogBookingResponseProcessStarted(
                $"Start the booking response processing with the reference code '{bookingResponse.ReferenceCode}'. Old status: {booking.Status}");

            if (bookingResponse.Status == BookingStatusCodes.NotFound)
            {
                await _bookingChangesProcessor.ProcessBookingNotFound(booking, bookingResponse);
                return;
            }

            if (bookingResponse.Status.ToInternalStatus() == booking.Status)
            {
                _logger.LogBookingResponseProcessSuccess(
                    $"The booking response with the reference code '{bookingResponse.ReferenceCode}' has been successfully processed. No changes applied");
                return;
            }

            await UpdateBookingDetails();

            switch (bookingResponse.Status)
            {
                case BookingStatusCodes.Confirmed:
                    await _bookingChangesProcessor.ProcessConfirmation(booking, bookingResponse);
                    break;
                case BookingStatusCodes.Cancelled:
                    await _bookingChangesProcessor.ProcessCancellation(booking, UserInfo.InternalServiceAccount);
                    break;
            }

            _logger.LogBookingResponseProcessSuccess(
                $"The booking response with the reference code '{bookingResponse.ReferenceCode}' has been successfully processed. " +
                $"New status: {bookingResponse.Status}");


            Task UpdateBookingDetails() => _bookingRecordsManager.UpdateBookingDetails(bookingResponse, booking);
            
            //TICKET https://happytravel.atlassian.net/browse/NIJO-315
            /*
            async Task<Result> LogAppliedMarkups()
            {
                long availabilityId = ??? ;
                
                var (_, isGetAvailabilityFailure, responseWithMarkup, cachedAvailabilityError) = await _availabilityResultsCache.Get(availabilityId);
                if (isGetAvailabilityFailure)
                    return Result.Fail(cachedAvailabilityError);

                await _markupLogger.Write(bookingResponse.ReferenceCode, ServiceTypes.HTL, responseWithMarkup.AppliedPolicies);
                return Result.Success();
            }
            */
        }
        
        private readonly IBookingAuditLogService _bookingAuditLogService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingChangesProcessor _bookingChangesProcessor;
        private readonly ILogger<BookingResponseProcessor> _logger;
    }
}