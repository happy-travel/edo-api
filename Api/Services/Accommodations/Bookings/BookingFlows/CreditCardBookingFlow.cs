using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Payments.CreditCards;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class CreditCardBookingFlow
    {
        public CreditCardBookingFlow(IBookingEvaluationStorage bookingEvaluationStorage,
            IBookingMailingService bookingMailingService,
            IBookingRecordsManager bookingRecordsManager,
            IBookingRequestStorage requestStorage,
            ICreditCardPaymentProcessingService creditCardPaymentProcessingService,
            IBookingResponseProcessor responseProcessor,
            IBookingPaymentService bookingPaymentService,
            BookingRequestExecutor bookingRequestExecutor,
            IDateTimeProvider dateTimeProvider,
            RestrictedRateChecker rateChecker)
        {
            _bookingEvaluationStorage = bookingEvaluationStorage;
            _bookingMailingService = bookingMailingService;
            _bookingRecordsManager = bookingRecordsManager;
            _requestStorage = requestStorage;
            _creditCardPaymentProcessingService = creditCardPaymentProcessingService;
            _responseProcessor = responseProcessor;
            _bookingPaymentService = bookingPaymentService;
            _bookingRequestExecutor = bookingRequestExecutor;
            _dateTimeProvider = dateTimeProvider;
            _rateChecker = rateChecker;
        }


        public async Task<Result<string>> Register(AccommodationBookingRequest bookingRequest, AgentContext agentContext, string languageCode)
        {
            return await GetCachedAvailabilityInfo(bookingRequest)
                .Check(CheckRateRestrictions)
                .Map(Register);

            Task<Result> CheckRateRestrictions(BookingAvailabilityInfo availabilityInfo) 
                => _rateChecker.CheckRateRestrictions(availabilityInfo, agentContext);


            async Task<string> Register(BookingAvailabilityInfo bookingAvailability)
            {
                var referenceCode = await _bookingRecordsManager.Register(bookingRequest, bookingAvailability, agentContext, languageCode);
                await _requestStorage.Set(referenceCode, (bookingRequest, bookingAvailability.AvailabilityId));
                return referenceCode;
            }


            Task<Result<BookingAvailabilityInfo>> GetCachedAvailabilityInfo(AccommodationBookingRequest request)
                => _bookingEvaluationStorage.Get(request.SearchId, request.ResultId, request.RoomContractSetId);
        }


        public async Task<Result<AccommodationBookingInfo>> Finalize(string referenceCode, AgentContext agentContext, string languageCode)
        {
            return await ProcessCreditCardPayment(referenceCode, agentContext)
                .Map(SendSupplierRequest)
                .Tap(ProcessResponse)
                .Bind(GetAccommodationBookingInfo);


            Task<Result<AccommodationBookingInfo>> GetAccommodationBookingInfo((EdoContracts.Accommodations.Booking, Booking booking) bookingInfo)
                => _bookingRecordsManager.GetAccommodationBookingInfo(bookingInfo.booking.ReferenceCode, languageCode);


            async Task<(EdoContracts.Accommodations.Booking, Booking)> SendSupplierRequest(Booking booking)
            {
                var (_, _, requestInfo, _) = await _requestStorage.Get(booking.ReferenceCode);
                var response = await _bookingRequestExecutor.SendSupplierRequest(requestInfo.request, requestInfo.availabilityId, booking, referenceCode, languageCode);
                return (response, booking);
            }


            Task ProcessResponse((EdoContracts.Accommodations.Booking response, Booking booking) bookingInfo)
                => _responseProcessor.ProcessResponse(bookingInfo.response, bookingInfo.booking);
        }

        
        public async Task<Result<Booking>> ProcessCreditCardPayment(string referenceCode, AgentContext agent)
        {
            return await GetBooking(referenceCode, agent)
                .Ensure(b => agent.AgencyId == b.AgencyId, "The booking does not belong to your current agency")
                .Check(CheckBookingIsAuthorized)
                .Check(CaptureMoneyIfDeadlinePassed)
                .Tap(NotifyAgent);


            Task<Result<Booking>> GetBooking(string referenceCode, AgentContext agent)
                => _bookingRecordsManager.GetAgentsBooking(referenceCode, agent);


            Result CheckBookingIsAuthorized(Booking bookingFromPipe)
                => bookingFromPipe.PaymentStatus == BookingPaymentStatuses.Authorized
                    ? Result.Failure("Only authorized bookings")
                    : Result.Success();


            async Task<Result> CaptureMoneyIfDeadlinePassed(Booking booking)
            {
                var daysBeforeDeadline = Infrastructure.Constants.Common.DaysBeforeDeadlineWhenPayForBooking;
                var now = _dateTimeProvider.UtcNow();

                var deadlinePassed = booking.CheckInDate <= now.AddDays(daysBeforeDeadline)
                    || (booking.DeadlineDate.HasValue && booking.DeadlineDate.Value.Date <= now.AddDays(daysBeforeDeadline));

                if (!deadlinePassed)
                    return Result.Success();

                var (_, isPaymentFailure, _, paymentError) = await _creditCardPaymentProcessingService.CaptureMoney(booking.ReferenceCode, agent.ToUserInfo(), _bookingPaymentService);
                if (isPaymentFailure)
                    return Result.Failure(paymentError);

                return Result.Success();
            }


            async Task NotifyAgent(Booking booking)
                => await _bookingMailingService.SendCreditCardPaymentNotifications(booking.ReferenceCode);
        }

        
        
        

        private readonly IBookingEvaluationStorage _bookingEvaluationStorage;
        private readonly IBookingMailingService _bookingMailingService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingRequestStorage _requestStorage;
        private readonly ICreditCardPaymentProcessingService _creditCardPaymentProcessingService;
        private readonly IBookingResponseProcessor _responseProcessor;
        private readonly IBookingPaymentService _bookingPaymentService;
        private readonly BookingRequestExecutor _bookingRequestExecutor;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly RestrictedRateChecker _rateChecker;
    }
}