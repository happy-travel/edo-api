using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Booking = HappyTravel.Edo.Data.Bookings.Booking;
using RoomContractSetAvailability = HappyTravel.EdoContracts.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class AccountBookingFlow 
    {
        public AccountBookingFlow(IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IBookingRecordsManager bookingRecordsManager,
            IBookingDocumentsService documentsService,
            IPaymentNotificationService notificationService,
            IDateTimeProvider dateTimeProvider,
            IAccountPaymentService accountPaymentService,
            IBookingEvaluationStorage bookingEvaluationStorage,
            IBookingResponseProcessor bookingResponseProcessor,
            IBookingRequestStorage requestStorage,
            RestrictedRateChecker rateChecker,
            BookingRequestExecutor requestExecutor)
        {
            _bookingRecordsManager = bookingRecordsManager;
            _documentsService = documentsService;
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
            _accountPaymentService = accountPaymentService;
            _bookingEvaluationStorage = bookingEvaluationStorage;
            _bookingResponseProcessor = bookingResponseProcessor;
            _requestStorage = requestStorage;
            _rateChecker = rateChecker;
            _requestExecutor = requestExecutor;
        }
        

        public async Task<Result<AccommodationBookingInfo>> BookByAccount(AccommodationBookingRequest bookingRequest,
            AgentContext agentContext, string languageCode, string clientIp)
        {
            return await GetCachedAvailabilityInfo(bookingRequest)
                .Check(CheckRateRestrictions)
                .Bind(RegisterBooking)
                .Bind(PayUsingAccountIfDeadlinePassed)
                .Bind(SendSupplierRequest)
                .Tap(ProcessResponse)
                .Bind(GetAccommodationBookingInfo);

            
            Task<Result> CheckRateRestrictions(BookingAvailabilityInfo availabilityInfo) 
                => _rateChecker.CheckRateRestrictions(availabilityInfo, agentContext);
            
            Task<Result<BookingAvailabilityInfo>> GetCachedAvailabilityInfo(AccommodationBookingRequest request)
                => _bookingEvaluationStorage.Get(request.SearchId, request.ResultId, request.RoomContractSetId);


                
            Task<Result<AccommodationBookingInfo>> GetAccommodationBookingInfo((EdoContracts.Accommodations.Booking, Booking booking) bookingInfo)
                => _bookingRecordsManager.GetAccommodationBookingInfo(bookingInfo.booking.ReferenceCode, languageCode);


            
            async Task<Result<(string referenceCode, BookingAvailabilityInfo)>> RegisterBooking(BookingAvailabilityInfo bookingAvailability)
            {
                var referenceCode = await _bookingRecordsManager.Register(bookingRequest, bookingAvailability, agentContext, languageCode);
                return (referenceCode, bookingAvailability);
            }



            async Task<Result<Booking>> PayUsingAccountIfDeadlinePassed((string referenceCode, BookingAvailabilityInfo availabilityInfo) bookingInfo)
            {
                var (refCode, availability) = bookingInfo;
                var bookingInPipeline = (await _bookingRecordsManager.Get(refCode)).Value;
                var daysBeforeDeadline = Infrastructure.Constants.Common.DaysBeforeDeadlineWhenPayForBooking;
                var now = _dateTimeProvider.UtcNow();

                var deadlinePassed = availability.CheckInDate <= now.AddDays(daysBeforeDeadline)
                    || (availability.RoomContractSet.Deadline.HasValue && availabilityDeadline <= now.AddDays(daysBeforeDeadline));

                if (!deadlinePassed)
                    return bookingInPipeline;

                var (_, isPaymentFailure, _, paymentError) = await _accountPaymentService.Charge(bookingInPipeline, agentContext, clientIp);
                if (isPaymentFailure)
                    return Result.Failure<Booking>(paymentError);

                return bookingInPipeline;
            }
            
            async Task<Result<(EdoContracts.Accommodations.Booking, Booking)>> SendSupplierRequest(Booking booking)
            {
                var (_, _, requestInfo, _) = await _requestStorage.Get(booking.ReferenceCode);
                var response = await _requestExecutor.SendSupplierRequest(requestInfo.request, requestInfo.availabilityId, booking, booking.ReferenceCode, languageCode);
                return (response, booking);
            }

            Task ProcessResponse((EdoContracts.Accommodations.Booking response, Booking booking) bookingInfo)
                => _bookingResponseProcessor.ProcessResponse(bookingInfo.response, bookingInfo.booking);
        }
        
        
        private async Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> SendReceipt(EdoContracts.Accommodations.Booking details, Booking booking, AgentContext agentContext)
        {
            var (_, isReceiptFailure, receiptInfo, receiptError) = await _documentsService.GenerateReceipt(booking.Id, agentContext.AgentId);
            if (isReceiptFailure)
                return ProblemDetailsBuilder.Fail<EdoContracts.Accommodations.Booking>(receiptError);

            await _notificationService.SendReceiptToCustomer(receiptInfo, agentContext.Email);
            return Result.Success<EdoContracts.Accommodations.Booking, ProblemDetails>(details);
        }

        
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly IBookingEvaluationStorage _bookingEvaluationStorage;
        private readonly IBookingResponseProcessor _bookingResponseProcessor;
        private readonly IBookingRequestStorage _requestStorage;
        private readonly RestrictedRateChecker _rateChecker;
        private readonly BookingRequestExecutor _requestExecutor;
    }
}