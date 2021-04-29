using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution.Flows
{
    public class BankCreditCardBookingFlow : IBankCreditCardBookingFlow
    {
        public BankCreditCardBookingFlow(IBookingRequestStorage requestStorage,
            IBookingNotificationService bookingNotificationService,
            IBookingRequestExecutor requestExecutor,
            IBookingEvaluationStorage evaluationStorage,
            IBookingCreditCardPaymentService creditCardPaymentService,
            IBookingDocumentsService documentsService,
            IBookingInfoService bookingInfoService,
            IDateTimeProvider dateTimeProvider,
            IBookingRegistrationService registrationService,
            ILogger<BankCreditCardBookingFlow> logger)
        {
            _requestStorage = requestStorage;
            _bookingNotificationService = bookingNotificationService;
            _requestExecutor = requestExecutor;
            _evaluationStorage = evaluationStorage;
            _creditCardPaymentService = creditCardPaymentService;
            _documentsService = documentsService;
            _bookingInfoService = bookingInfoService;
            _dateTimeProvider = dateTimeProvider;
            _registrationService = registrationService;
            _logger = logger;
        }
        
        
        public async Task<Result<string>> Register(AccommodationBookingRequest bookingRequest, AgentContext agentContext, string languageCode)
        {
            return await GetCachedAvailability(bookingRequest)
                .Ensure(IsPaymentTypeAllowed, "Payment type is not allowed")
                .Map(Register);


            async Task<Result<BookingAvailabilityInfo>> GetCachedAvailability(AccommodationBookingRequest bookingRequest)
                => await _evaluationStorage.Get(bookingRequest.SearchId, bookingRequest.ResultId, bookingRequest.RoomContractSetId);

                
            bool IsPaymentTypeAllowed(BookingAvailabilityInfo availabilityInfo) 
                => availabilityInfo.AvailablePaymentTypes.Contains(PaymentTypes.CreditCard);


            async Task<string> Register(BookingAvailabilityInfo bookingAvailability)
            {
                var booking = await _registrationService.Register(bookingRequest, bookingAvailability, PaymentTypes.CreditCard, agentContext, languageCode);
                await _requestStorage.Set(booking.ReferenceCode, (bookingRequest, bookingAvailability.AvailabilityId));
                return booking.ReferenceCode;
            }


            // TODO NIJO-1135: Revert logging in further refactoring steps
            // Result<string, ProblemDetails> WriteLog(Result<string, ProblemDetails> result)
            //     => LoggerUtils.WriteLogByResult(result,
            //         () => _logger.LogBookingRegistrationSuccess($"Successfully registered a booking with reference code: '{result.Value}'"),
            //         () => _logger.LogBookingRegistrationFailure($"Failed to register a booking. AvailabilityId: '{availabilityId}'. " +
            //             $"Itinerary number: {bookingRequest.ItineraryNumber}. Passenger name: {bookingRequest.MainPassengerName}. Error: {result.Error.Detail}"));
        }
        
        
        public async Task<Result<AccommodationBookingInfo>> Finalize(string referenceCode, AgentContext agentContext, string languageCode)
        {
            return await GetBooking()
                .Check(CheckBookingIsPaid)
                .CheckIf(IsDeadlinePassed, CaptureMoney)
                .Check(GenerateInvoice)
                .Bind(SendSupplierRequest)
                .Bind(NotifyPaymentReceived)
                .Bind(GetAccommodationBookingInfo);

            
            Task<Result<Booking>> GetBooking()
                => _bookingInfoService.GetAgentsBooking(referenceCode, agentContext);
            
            
            Result CheckBookingIsPaid(Booking bookingFromPipe)
            {
                if (bookingFromPipe.PaymentStatus != BookingPaymentStatuses.Authorized)
                {
                    _logger.LogBookingFinalizationPaymentFailure($"The booking with reference code: '{referenceCode}' hasn't been paid");
                    return Result.Failure<Booking>("The booking hasn't been paid");
                }

                return Result.Success();
            }

            
            bool IsDeadlinePassed(Booking booking) 
                => booking.GetPayDueDate() <= _dateTimeProvider.UtcToday();


            async Task<Result> CaptureMoney(Booking booking) 
                => await _creditCardPaymentService.Capture(booking, agentContext.ToUserInfo());
            

            async Task<Result<EdoContracts.Accommodations.Booking>> SendSupplierRequest(Data.Bookings.Booking booking)
            {
                var (_, isFailure, requestInfo, error) = await _requestStorage.Get(booking.ReferenceCode);
                if(isFailure)
                    return Result.Failure<EdoContracts.Accommodations.Booking>(error);

                var (request, availabilityId) = requestInfo;
                return await _requestExecutor.Execute(request, availabilityId, booking, agentContext, languageCode);
            }

            
            Task<Result> GenerateInvoice(Data.Bookings.Booking booking) 
                => _documentsService.GenerateInvoice(booking);


            async Task<Result<EdoContracts.Accommodations.Booking>> NotifyPaymentReceived(EdoContracts.Accommodations.Booking details)
            {
                await _bookingNotificationService.NotifyCreditCardPaymentConfirmed(details.ReferenceCode);
                return details;
            }


            Task<Result<AccommodationBookingInfo>> GetAccommodationBookingInfo(EdoContracts.Accommodations.Booking details)
                => _bookingInfoService.GetAccommodationBookingInfo(details.ReferenceCode, languageCode);
        }
        
        
        private readonly IBookingRequestStorage _requestStorage;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly IBookingRequestExecutor _requestExecutor;
        private readonly IBookingEvaluationStorage _evaluationStorage;
        private readonly IBookingCreditCardPaymentService _creditCardPaymentService;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IBookingInfoService _bookingInfoService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingRegistrationService _registrationService;
        private readonly ILogger<BankCreditCardBookingFlow> _logger;
    }
}