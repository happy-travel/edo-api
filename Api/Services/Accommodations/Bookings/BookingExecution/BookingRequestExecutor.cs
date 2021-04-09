using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.Errors;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution
{
    public class BookingRequestExecutor : IBookingRequestExecutor
    {
        public BookingRequestExecutor(ISupplierConnectorManager supplierConnectorManager,
            IBookingResponseProcessor responseProcessor,
            AvailabilityAnalyticsService analyticsService,
            IBookingRecordsUpdater bookingRecordsUpdater,
            IDateTimeProvider dateTimeProvider,
            ILogger<BookingRequestExecutor> logger)
        {
            _supplierConnectorManager = supplierConnectorManager;
            _responseProcessor = responseProcessor;
            _analyticsService = analyticsService;
            _bookingRecordsUpdater = bookingRecordsUpdater;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }


        public async Task<Result<Booking>> Execute(AccommodationBookingRequest bookingRequest, string availabilityId, Data.Bookings.Booking booking, AgentContext agent, string languageCode)
        {
            var bookingRequestResult = await SendSupplierRequest(bookingRequest, availabilityId, booking, languageCode);
            if (bookingRequestResult.IsSuccess)
                _analyticsService.LogBookingOccured(bookingRequest, booking, agent);
            
            await ProcessRequestResult(bookingRequestResult);
            return bookingRequestResult;
            
            async Task<Result<Booking>> SendSupplierRequest(AccommodationBookingRequest bookingRequest, string availabilityId,
                Data.Bookings.Booking booking, string languageCode)
            {
                var features = new List<Feature>();

                var roomDetails = bookingRequest.RoomDetails
                    .Select(d => new SlimRoomOccupation(d.Type, d.Passengers, string.Empty, d.IsExtraBedNeeded))
                    .ToList();

                var innerRequest = new BookingRequest(availabilityId,
                    bookingRequest.RoomContractSetId,
                    booking.ReferenceCode,
                    roomDetails,
                    features,
                    bookingRequest.RejectIfUnavailable);

                try
                {
                    var (isSuccess, _, bookingResult, error) = await _supplierConnectorManager
                        .Get(booking.Supplier)
                        .Book(innerRequest, languageCode);

                    if (isSuccess)
                        return bookingResult;

                    var message = error.Detail;
                    // If result is failed this does not mean that booking failed. All known cases are listed below
                    _logger.LogBookingFinalizationFailure($"The booking finalization with the reference code: '{booking.ReferenceCode}' has been failed with a message: {message}");

                    if (!error.Extensions.TryGetBookingFailureCode(out var failureCode))
                        // We do not know whether booking was registered on supplier
                        return GetStubDetails(booking);
                    
                    return failureCode switch
                    {
                        // We are sure that booking was not done
                        BookingFailureCodes.ConnectorValidationFailed => Result.Failure<Booking>(message),
                        BookingFailureCodes.ValuationResultNotFound => Result.Failure<Booking>(message),
                        BookingFailureCodes.PreBookingFailed => Result.Failure<Booking>(message),
                        BookingFailureCodes.SupplierValidationFailed => Result.Failure<Booking>(message),
                        BookingFailureCodes.SupplierRejected => Result.Failure<Booking>(message),
                        
                        // We do not know whether booking was registered on supplier
                        _ => GetStubDetails(booking)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogBookingFinalizationException(ex);
                    return GetStubDetails(booking);
                }


                // TODO: Remove room information and contract description from booking NIJO-915
                static EdoContracts.Accommodations.Booking GetStubDetails(Data.Bookings.Booking booking)
                    => new EdoContracts.Accommodations.Booking(booking.ReferenceCode,
                        // Will be set in the refresh step
                        BookingStatusCodes.WaitingForResponse,
                        booking.AccommodationId,
                        booking.SupplierReferenceCode,
                        booking.CheckInDate,
                        booking.CheckOutDate,
                        new List<SlimRoomOccupation>(0),
                        booking.UpdateMode);
            }
            
            
            async Task ProcessRequestResult(Result<Booking> responseResult)
            {
                if (responseResult.IsSuccess)
                {
                    await _responseProcessor.ProcessResponse(responseResult.Value, agent.ToUserInfo(), BookingChangeEvents.BookingRequest);
                }
                else
                {
                    var changeReason = new Data.Bookings.BookingChangeReason
                    {
                        Source = BookingChangeSources.System,
                        Event = BookingChangeEvents.BookingRequest
                    };
                    
                    await _bookingRecordsUpdater.ChangeStatus(booking, BookingStatuses.Invalid, _dateTimeProvider.UtcNow(), 
                        ApiCaller.InternalServiceAccount, changeReason);
                }
            }
        }


        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IBookingResponseProcessor _responseProcessor;
        private readonly AvailabilityAnalyticsService _analyticsService;
        private readonly IBookingRecordsUpdater _bookingRecordsUpdater;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<BookingRequestExecutor> _logger;
    }
}