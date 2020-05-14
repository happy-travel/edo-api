using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingService : IBookingService
    {
        public BookingService(IAvailabilityResultsCache availabilityResultsCache,
            IBookingRecordsManager bookingRecordsManager,
            IBookingAuditLogService bookingAuditLogService,
            ISupplierOrderService supplierOrderService,
            EdoContext context,
            IBookingMailingService bookingMailingService,
            ILogger<BookingService> logger,
            IProviderRouter providerRouter,
            IServiceAccountContext serviceAccountContext,
            IAgentContext agentContext,
            IBookingPaymentService paymentService)
        {
            _availabilityResultsCache = availabilityResultsCache;
            _bookingRecordsManager = bookingRecordsManager;
            _bookingAuditLogService = bookingAuditLogService;
            _supplierOrderService = supplierOrderService;
            _context = context;
            _bookingMailingService = bookingMailingService;
            _logger = logger;
            _providerRouter = providerRouter;
            _serviceAccountContext = serviceAccountContext;
            _agentContext = agentContext;
            _paymentService = paymentService;
        }

        
        public async Task<Result<string, ProblemDetails>> Register(AccommodationBookingRequest bookingRequest, RequestMetadata requestMetadata)
        {
            var (_, isCachedAvailabilityFailure, responseWithMarkup, cachedAvailabilityError) = await _availabilityResultsCache.Get(bookingRequest.DataProvider, bookingRequest.AvailabilityId);
            if (isCachedAvailabilityFailure)
                return ProblemDetailsBuilder.Fail<string>(cachedAvailabilityError);
            
            var bookingAvailability = ExtractBookingAvailabilityInfo(responseWithMarkup.Data);
            
            var referenceCode = await _bookingRecordsManager.Register(bookingRequest, bookingAvailability, requestMetadata.LanguageCode);
            return Result.Ok<string, ProblemDetails>(referenceCode);
        }
        
        
        public async Task<Result<BookingDetails, ProblemDetails>> Finalize(string referenceCode, RequestMetadata requestMetadata)
        {
            var (_, isFailure, booking, error) = await _bookingRecordsManager.GetAgentsBooking(referenceCode);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(error);

            if (booking.PaymentStatus == BookingPaymentStatuses.NotPaid)
            {
                _logger.LogBookingFinalizationFailedToPay($"The booking with the reference code: '{referenceCode}' hasn't been paid");
                return ProblemDetailsBuilder.Fail<BookingDetails>("The booking hasn't been paid");
            }

            return await SendBookingRequest()
                .OnSuccess(details => ProcessResponse(details, booking, requestMetadata))
                .OnFailure(VoidMoney);

         
            async Task<Result<BookingDetails, ProblemDetails>> SendBookingRequest()
            {
                try
                {
                    // TODO: will be implemented in NIJO-31 
                    var bookingRequest = JsonConvert.DeserializeObject<AccommodationBookingRequest>(booking.BookingRequest);

                    var features = new List<Feature>(); //bookingRequest.Features

                    var roomDetails = bookingRequest.RoomDetails
                        .Select(d => new SlimRoomDetails(d.Type, d.Passengers, d.IsExtraBedNeeded))
                        .ToList();

                    var innerRequest = new BookingRequest(bookingRequest.AvailabilityId,
                        bookingRequest.RoomContractSetId,
                        bookingRequest.Nationality,
                        bookingRequest.PaymentMethod,
                        booking.ReferenceCode,
                        bookingRequest.Residency,
                        roomDetails,
                        features,
                        bookingRequest.RejectIfUnavailable);

                    var bookingResult = await _providerRouter.Book(booking.DataProvider, innerRequest, requestMetadata);
                    if(bookingResult.IsFailure)
                        _logger.LogBookingFinalizationFailed($"The booking finalization with the reference code: '{referenceCode}' has been failed");

                    return bookingResult;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Failed to update booking data (refcode '{referenceCode}') after the request to the connector";

                    var (_, isCancellationFailed, cancellationError) = await _providerRouter.CancelBooking(booking.DataProvider, booking.ReferenceCode, requestMetadata);
                    if (isCancellationFailed)
                        errorMessage += Environment.NewLine + $"Booking cancellation has failed: {cancellationError}";

                    _logger.LogBookingFinalizationFailed(errorMessage);

                    return ProblemDetailsBuilder.Fail<BookingDetails>(
                        $"Cannot update booking data (refcode '{referenceCode}') after the request to the connector");
                }
            }


            Task VoidMoney(ProblemDetails problemDetails) => _paymentService.VoidMoney(booking);
        }
        
        
        public async Task ProcessResponse(BookingDetails bookingResponse, Booking booking, RequestMetadata requestMetadata)
        {
            if (bookingResponse.Status == booking.Status)
                return;
            
            await _bookingAuditLogService.Add(bookingResponse, booking);
            
            _logger.LogBookingProcessResponseStarted($"Start the booking response processing with the reference code '{bookingResponse.ReferenceCode}'");
            
            switch (bookingResponse.Status)
            {
                case BookingStatusCodes.Confirmed:
                    await ConfirmBooking();
                    break;
                case BookingStatusCodes.Cancelled:
                    await CancelBooking(booking, requestMetadata);
                    break;
                default: 
                    await UpdateBookingDetails();
                    break;
            }

            _logger.LogBookingProcessResponseSuccess(
                $"The booking response with the reference code '{bookingResponse.ReferenceCode}' has been successfully processed");
            
            async Task ConfirmBooking()
            {
                await _bookingRecordsManager.Confirm(bookingResponse, booking);
                await SaveSupplierOrder();
            }


            Task UpdateBookingDetails() => _bookingRecordsManager.UpdateBookingDetails(bookingResponse, booking);

            
            async Task SaveSupplierOrder()
            {
                var supplierPrice = bookingResponse.RoomContractSet.Price.NetTotal;
                await _supplierOrderService.Add(bookingResponse.ReferenceCode, ServiceTypes.HTL, supplierPrice);
            }
            
            //TICKET https://happytravel.atlassian.net/browse/NIJO-315
            /*
            async Task<Result> LogAppliedMarkups()
            {
                long availabilityId = ??? ;
                
                var (_, isGetAvailabilityFailure, responseWithMarkup, cachedAvailabilityError) = await _availabilityResultsCache.Get(availabilityId);
                if (isGetAvailabilityFailure)
                    return Result.Fail(cachedAvailabilityError);

                await _markupLogger.Write(bookingResponse.ReferenceCode, ServiceTypes.HTL, responseWithMarkup.AppliedPolicies);
                return Result.Ok();
            }
            */
        }
        
        private async Task CancelBooking(Booking booking, RequestMetadata requestMetadata)
        {
            await _bookingRecordsManager.ConfirmBookingCancellation(booking);
            await NotifyAgent();
            await CancelSupplierOrder();
            
            async Task CancelSupplierOrder()
            {
                var referenceCode = booking.ReferenceCode;
                await _supplierOrderService.Cancel(referenceCode);
            }
            

            async Task NotifyAgent()
            {
                var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == booking.AgentId);
                if (agent == default)
                {
                    _logger.LogWarning("Booking cancellation notification: could not find agent with id '{0}' for the booking '{1}'",
                        booking.AgentId, booking.ReferenceCode);
                    return;
                }

                await _bookingMailingService.NotifyBookingCancelled(booking.ReferenceCode, agent.Email, $"{agent.LastName} {agent.FirstName}");
            }
        }
        
      
        public async Task<Result<VoidObject, ProblemDetails>> Cancel(int bookingId, RequestMetadata requestMetadata)
        {
            UserInfo userInfo;
            var userInfoResult  = await _serviceAccountContext.GetUserInfo();
            if (userInfoResult.IsSuccess)
            {
                userInfo = userInfoResult.Value;
            }
            else
            {
                userInfoResult = await _agentContext.GetUserInfo();
                if (userInfoResult.IsFailure)
                    return ProblemDetailsBuilder.Fail<VoidObject>(userInfoResult.Error);

                userInfo = userInfoResult.Value;
            }
            
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingId, userInfo.Id);
            if (isGetBookingFailure)
                return ProblemDetailsBuilder.Fail<VoidObject>(getBookingError);

            return await ProcessBookingCancellation(booking, requestMetadata);
        }
        
        
        public async Task<Result<BookingDetails, ProblemDetails>> RefreshStatus(int bookingId, RequestMetadata requestMetadata)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await _bookingRecordsManager.Get(bookingId);
            if (isGetBookingFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(getBookingError);

            var refCode = booking.ReferenceCode;
            var (_, isGetDetailsFailure, newDetails, getDetailsError) = await _providerRouter.GetBookingDetails(booking.DataProvider, refCode, requestMetadata);
            if(isGetDetailsFailure)
                return Result.Fail<BookingDetails, ProblemDetails>(getDetailsError);
            
            await _bookingRecordsManager.UpdateBookingDetails(newDetails, booking);
            return Result.Ok<BookingDetails, ProblemDetails>(newDetails);
        }


        private async Task<Result<VoidObject, ProblemDetails>> ProcessBookingCancellation(Booking booking, RequestMetadata requestMetadata)
        {
            if (booking.Status == BookingStatusCodes.Cancelled)
                return Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);
            
            var (_, isFailure, _, error) = await SendCancellationRequest()
                .OnSuccess(VoidMoney)
                .OnSuccess(SetBookingCancelled);

            return isFailure
                ? ProblemDetailsBuilder.Fail<VoidObject>(error.Detail)
                : Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);


            Task SetBookingCancelled(Booking b) => _bookingRecordsManager.ConfirmBookingCancellation(b);


            async Task<Result<Booking, ProblemDetails>> SendCancellationRequest()
            {
                var (_, isCancelFailure, _, cancelError) = await _providerRouter.CancelBooking(booking.DataProvider, booking.ReferenceCode, requestMetadata);
                return isCancelFailure
                    ? Result.Fail<Booking, ProblemDetails>(cancelError)
                    : Result.Ok<Booking, ProblemDetails>(booking);
            }


            async Task<Result<Booking, ProblemDetails>> VoidMoney(Booking b)
            {
                var (_, isVoidMoneyFailure, voidError) = await _paymentService.VoidMoney(b);

                return isVoidMoneyFailure
                    ? ProblemDetailsBuilder.Fail<Booking>(voidError)
                    : Result.Ok<Booking, ProblemDetails>(b);
            }
        }


        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(SingleAccommodationAvailabilityDetailsWithDeadline response)
        {
            var location = response.AccommodationDetails.Location;
            
            return new BookingAvailabilityInfo(
                response.AccommodationDetails.Id,
                response.AccommodationDetails.Name,
                response.RoomContractSet,
                location.LocalityZoneCode,
                location.LocalityZone,
                location.LocalityCode,
                location.Locality,
                location.CountryCode,
                location.Country,
                location.Address,
                location.Coordinates,
                response.CheckInDate,
                response.CheckOutDate);
        }
        
        
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingAuditLogService _bookingAuditLogService;
        private readonly ISupplierOrderService _supplierOrderService;
        private readonly EdoContext _context;
        private readonly IBookingMailingService _bookingMailingService;
        private readonly ILogger<BookingService> _logger;
        private readonly IProviderRouter _providerRouter;
        private readonly IServiceAccountContext _serviceAccountContext;
        private readonly IAgentContext _agentContext;
        private readonly IBookingPaymentService _paymentService;
    }
}