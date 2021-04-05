using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    // TODO: Rename to SupplierBookingManagementService
    public class BookingManagementService : IBookingManagementService
    {
        public BookingManagementService(IBookingRecordsUpdater bookingRecordsUpdater,
            ILogger<BookingManagementService> logger,
            ISupplierConnectorManager supplierConnectorFactory,
            IDateTimeProvider dateTimeProvider,
            IBookingResponseProcessor responseProcessor)
        {
            _bookingRecordsUpdater = bookingRecordsUpdater;
            _logger = logger;
            _supplierConnectorManager = supplierConnectorFactory;
            _dateTimeProvider = dateTimeProvider;
            _responseProcessor = responseProcessor;
        }
        
        
        public async Task<Result> Cancel(Booking booking, UserInfo user, BookingChangeReason changeReason)
        {
            if (booking.Status == BookingStatuses.Cancelled)
            {
                _logger.LogBookingAlreadyCancelled(
                    $"Skipping cancellation for a booking with reference code: '{booking.ReferenceCode}'. Already cancelled.");
                
                return Result.Success();
            }

            return await CheckBookingCanBeCancelled()
                .Bind(SendCancellationRequest)
                .Bind(ProcessCancellation)
                .Finally(WriteLog);


            Result CheckBookingCanBeCancelled()
            {
                if(booking.Status != BookingStatuses.Confirmed)
                    return Result.Failure("Only confirmed bookings can be cancelled");
                
                if (booking.CheckInDate <= _dateTimeProvider.UtcToday())
                    return Result.Failure("Cannot cancel booking after check in date");

                return Result.Success();
            }


            async Task<Result<Booking>> SendCancellationRequest()
            {
                var (_, isCancelFailure, _, cancelError) = await _supplierConnectorManager.Get(booking.Supplier).CancelBooking(booking.ReferenceCode);
                return isCancelFailure
                    ? Result.Failure<Booking>(cancelError.Detail)
                    : Result.Success(booking);
            }

            
            async Task<Result> ProcessCancellation(Booking b)
            {
                await _bookingRecordsUpdater.ChangeStatus(b, BookingStatuses.PendingCancellation, _dateTimeProvider.UtcNow(), user, changeReason);

                return b.UpdateMode == BookingUpdateModes.Synchronous
                    ? await RefreshStatus(b, user, changeReason)
                    : Result.Success();
            }


            Result WriteLog(Result result)
                => LoggerUtils.WriteLogByResult(result,
                    () => _logger.LogBookingCancelSuccess($"Successfully cancelled a booking with reference code: '{booking.ReferenceCode}'"),
                    () => _logger.LogBookingCancelFailure(
                        $"Failed to cancel a booking with reference code: '{booking.ReferenceCode}'. Error: {result.Error}"));
        }



        public async Task<Result> RefreshStatus(Booking booking, UserInfo user, BookingChangeReason changeReason)
        {
            var oldStatus = booking.Status;
            var referenceCode = booking.ReferenceCode;
            var (_, isGetDetailsFailure, newDetails, getDetailsError) = await _supplierConnectorManager
                .Get(booking.Supplier)
                .GetBookingDetails(referenceCode, booking.LanguageCode);

            if (isGetDetailsFailure)
            {
                _logger.LogBookingRefreshStatusFailure($"Failed to refresh status for a booking with reference code: '{referenceCode}' " +
                    $"while getting info from a supplier. Error: {getDetailsError}");
                
                return Result.Failure(getDetailsError.Detail);
            }

            await _responseProcessor.ProcessResponse(newDetails, user, changeReason);

            _logger.LogBookingRefreshStatusSuccess($"Successfully refreshed status for a booking with reference code: '{referenceCode}'. " +
                $"Old status: {oldStatus}. New status: {newDetails.Status}");

            return Result.Success();
        }


        private readonly IBookingRecordsUpdater _bookingRecordsUpdater;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingResponseProcessor _responseProcessor;
        private readonly ILogger<BookingManagementService> _logger;
    }
}