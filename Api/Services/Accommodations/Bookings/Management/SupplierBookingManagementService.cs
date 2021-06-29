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
    public class SupplierBookingManagementService : ISupplierBookingManagementService
    {
        public SupplierBookingManagementService(IBookingRecordsUpdater bookingRecordsUpdater,
            ILogger<SupplierBookingManagementService> logger,
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
        
        
        public async Task<Result> Cancel(Booking booking, ApiCaller apiCaller, BookingChangeEvents eventType)
        {
            if (booking.Status == BookingStatuses.Cancelled)
            {
                _logger.LogBookingAlreadyCancelled(booking.ReferenceCode);
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
                var changeReason = new BookingChangeReason
                {
                    Event = eventType,
                    Source = BookingChangeSources.Supplier
                };
                
                await _bookingRecordsUpdater.ChangeStatus(b, BookingStatuses.PendingCancellation, _dateTimeProvider.UtcNow(), apiCaller, changeReason);
                
                return b.UpdateMode == BookingUpdateModes.Synchronous
                    ? await RefreshStatus(b, apiCaller, eventType)
                    : Result.Success();
            }


            Result WriteLog(Result result)
                => LoggerUtils.WriteLogByResult(result,
                    () => _logger.LogBookingCancelSuccess(booking.ReferenceCode),
                    () => _logger.LogBookingCancelFailure(booking.ReferenceCode, result.Error));
        }



        public async Task<Result> RefreshStatus(Booking booking, ApiCaller apiCaller, BookingChangeEvents eventType)
        {
            var oldStatus = booking.Status;
            var referenceCode = booking.ReferenceCode;
            var (_, isGetDetailsFailure, newDetails, getDetailsError) = await _supplierConnectorManager
                .Get(booking.Supplier)
                .GetBookingDetails(referenceCode, booking.LanguageCode);

            if (isGetDetailsFailure)
            {
                _logger.LogBookingRefreshStatusFailure(referenceCode, getDetailsError.Detail);
                return Result.Failure(getDetailsError.Detail);
            }

            await _responseProcessor.ProcessResponse(newDetails, apiCaller, eventType);

            _logger.LogBookingRefreshStatusSuccess(referenceCode, oldStatus, newDetails.Status);

            return Result.Success();
        }


        private readonly IBookingRecordsUpdater _bookingRecordsUpdater;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingResponseProcessor _responseProcessor;
        private readonly ILogger<SupplierBookingManagementService> _logger;
    }
}