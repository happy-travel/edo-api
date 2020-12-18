using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingManagementService : IBookingManagementService
    {
        public BookingManagementService(IBookingRecordsManager bookingRecordsManager,
            ILogger<BookingManagementService> logger,
            ISupplierConnectorManager supplierConnectorFactory,
            IBookingChangesProcessor bookingChangesProcessor,
            IBookingResponseProcessor responseProcessor)
        {
            _bookingRecordsManager = bookingRecordsManager;
            _logger = logger;
            _supplierConnectorManager = supplierConnectorFactory;
            _bookingChangesProcessor = bookingChangesProcessor;
            _responseProcessor = responseProcessor;
        }


        public async Task<Result<Unit, ProblemDetails>> Cancel(int bookingId, AgentContext agent)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingId, agent.AgentId);
            if (isGetBookingFailure)
                return ProblemDetailsBuilder.Fail<Unit>(getBookingError);

            // TODO Check up booking cancel permissions NIJO-1076
            if (agent.AgencyId != booking.AgencyId)
                return ProblemDetailsBuilder.Fail<Unit>("The booking does not belong to your current agency");

            return await CancelBooking(booking, agent.ToUserInfo());
        }


        public async Task<Result<Unit, ProblemDetails>> Cancel(int bookingId, ServiceAccount serviceAccount)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingId);
            if (isGetBookingFailure)
                return ProblemDetailsBuilder.Fail<Unit>(getBookingError);

            return await CancelBooking(booking, serviceAccount.ToUserInfo());
        }


        public async Task<Result<Unit, ProblemDetails>> Cancel(int bookingId, Administrator administrator, bool requireProviderConfirmation)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingId);
            if (isGetBookingFailure)
                return ProblemDetailsBuilder.Fail<Unit>(getBookingError);

            return await CancelBooking(booking, administrator.ToUserInfo(), requireProviderConfirmation);
        }


        public async Task<Result<Unit, ProblemDetails>> RefreshStatus(int bookingId)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingId);
            if (isGetBookingFailure)
            {
                _logger.LogBookingRefreshStatusFailure(
                    $"Failed to refresh status for a booking with id {bookingId} while getting the booking. Error: {getBookingError}");
                
                return ProblemDetailsBuilder.Fail<Unit>(getBookingError);
            }

            var oldStatus = booking.Status;
            var referenceCode = booking.ReferenceCode;
            var (_, isGetDetailsFailure, newDetails, getDetailsError) = await _supplierConnectorManager
                .Get(booking.Supplier)
                .GetBookingDetails(referenceCode, booking.LanguageCode);

            if (isGetDetailsFailure)
            {
                _logger.LogBookingRefreshStatusFailure($"Failed to refresh status for a booking with reference code: '{referenceCode}' " +
                    $"while getting info from a supplier. Error: {getBookingError}");
                
                return Result.Failure<Unit, ProblemDetails>(getDetailsError);
            }

            await _responseProcessor.ProcessResponse(newDetails, booking);

            _logger.LogBookingRefreshStatusSuccess($"Successfully refreshed status fot a booking with reference code: '{referenceCode}'. " +
                $"Old status: {oldStatus}. New status: {newDetails.Status}");

            return Unit.Instance;
        }


        private async Task<Result<Unit, ProblemDetails>> CancelBooking(Booking booking, UserInfo user,
            bool requireProviderConfirmation = true)
        {
            if (booking.Status == BookingStatuses.Cancelled)
            {
                _logger.LogBookingAlreadyCancelled(
                    $"Skipping cancellation for a booking with reference code: '{booking.ReferenceCode}'. Already cancelled.");
                
                return Result.Success<Unit, ProblemDetails>(Unit.Instance);
            }

            return await CheckBookingCanBeCancelled()
                .Bind(SendCancellationRequest)
                .Bind(ProcessCancellation)
                .Finally(WriteLog);


            Result<Unit, ProblemDetails> CheckBookingCanBeCancelled()
                => booking.Status == BookingStatuses.Confirmed
                    ? Unit.Instance
                    : ProblemDetailsBuilder.Fail<Unit>("Only confirmed bookings can be cancelled");


            async Task<Result<Booking, ProblemDetails>> SendCancellationRequest(Unit _)
            {
                var (_, isCancelFailure, _, cancelError) = await _supplierConnectorManager.Get(booking.Supplier).CancelBooking(booking.ReferenceCode);
                return isCancelFailure && requireProviderConfirmation
                    ? Result.Failure<Booking, ProblemDetails>(cancelError)
                    : Result.Success<Booking, ProblemDetails>(booking);
            }

            
            async Task<Result<Unit, ProblemDetails>> ProcessCancellation(Booking b)
            {
                return b.UpdateMode switch
                {
                    BookingUpdateModes.Synchronous => await _bookingChangesProcessor.ProcessCancellation(b, user).ToResultWithProblemDetails(),
                    BookingUpdateModes.Asynchronous => await _bookingRecordsManager.SetStatus(b, BookingStatuses.PendingCancellation).ToSuccessResultWithProblemDetails(),
                    _ => throw new ArgumentOutOfRangeException(nameof(b.UpdateMode))
                };
            }


            Result<T, ProblemDetails> WriteLog<T>(Result<T, ProblemDetails> result)
                => LoggerUtils.WriteLogByResult(result,
                    () => _logger.LogBookingCancelSuccess($"Successfully cancelled a booking with reference code: '{booking.ReferenceCode}'"),
                    () => _logger.LogBookingCancelFailure(
                        $"Failed to cancel a booking with reference code: '{booking.ReferenceCode}'. Error: {result.Error.Detail}"));
        }


        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IBookingChangesProcessor _bookingChangesProcessor;
        private readonly IBookingResponseProcessor _responseProcessor;
        private readonly ILogger<BookingManagementService> _logger;
    }
}