using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Metrics;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution.Flows
{
    public class FinancialAccountBookingFlow : IFinancialAccountBookingFlow
    {
        public FinancialAccountBookingFlow(IDateTimeProvider dateTimeProvider,
            IBookingAccountPaymentService accountPaymentService,
            IBookingEvaluationStorage bookingEvaluationStorage,
            IBookingDocumentsService documentsService,
            IBookingInfoService bookingInfoService,
            IBookingRegistrationService registrationService,
            IBookingRequestExecutor requestExecutor)
        {
            _dateTimeProvider = dateTimeProvider;
            _accountPaymentService = accountPaymentService;
            _bookingEvaluationStorage = bookingEvaluationStorage;
            _documentsService = documentsService;
            _bookingInfoService = bookingInfoService;
            _registrationService = registrationService;
            _requestExecutor = requestExecutor;
        }
        
        

        public Task<Result<AccommodationBookingInfo>> BookByAccount(AccommodationBookingRequest bookingRequest,
            AgentContext agentContext, string languageCode, string clientIp)
        {
            return GetCachedAvailability(bookingRequest)
                .Ensure(IsPaymentTypeAllowed, "Payment type is not allowed")
                .Map(RegisterBooking)
                .Check(GenerateInvoice)
                .CheckIf(IsDeadlinePassed, ChargeMoney)
                .Bind(SendSupplierRequest)
                .Bind(GetAccommodationBookingInfo)
                .Tap(IncreaseCounter);


            bool IsDeadlinePassed((Data.Bookings.Booking booking, BookingAvailabilityInfo) bookingInfo)
                => bookingInfo.booking.GetPayDueDate() <= _dateTimeProvider.UtcToday();


            async Task<Result<BookingAvailabilityInfo>> GetCachedAvailability(AccommodationBookingRequest bookingRequest)
                => await _bookingEvaluationStorage.Get(bookingRequest.SearchId,
                    bookingRequest.ResultId,
                    bookingRequest.RoomContractSetId);
            

            bool IsPaymentTypeAllowed(BookingAvailabilityInfo availabilityInfo) 
                => availabilityInfo.AvailablePaymentTypes.Contains(PaymentTypes.VirtualAccount);


            async Task<(Data.Bookings.Booking, BookingAvailabilityInfo)> RegisterBooking(BookingAvailabilityInfo bookingAvailability)
            {
                var booking = await _registrationService.Register(bookingRequest, bookingAvailability, PaymentTypes.VirtualAccount, agentContext, languageCode);
                return (booking, bookingAvailability);
            }

            
            async Task<Result> ChargeMoney((Data.Bookings.Booking, BookingAvailabilityInfo) bookingInfo)
            {
                var (booking, _) = bookingInfo;
                return await _accountPaymentService.Charge(booking, agentContext.ToApiCaller());
            }
            
            
            Task<Result> GenerateInvoice((Data.Bookings.Booking, BookingAvailabilityInfo) bookingInfo)
            {
                var (booking, _) = bookingInfo;
                return _documentsService.GenerateInvoice(booking);
            }


            async Task<Result<Booking>> SendSupplierRequest((Data.Bookings.Booking, BookingAvailabilityInfo) bookingInfo)
            {
                var (booking, availabilityInfo) = bookingInfo;
                return await _requestExecutor.Execute(bookingRequest, 
                    availabilityInfo.AvailabilityId,
                    booking,
                    agentContext,
                    languageCode);
            }


            Task<Result<AccommodationBookingInfo>> GetAccommodationBookingInfo(EdoContracts.Accommodations.Booking details)
                => _bookingInfoService.GetAccommodationBookingInfo(details.ReferenceCode, languageCode);


            static void IncreaseCounter()
                => Counters.BookingCreation.Inc();
            
            
            // TODO NIJO-1135: Revert logging in further refactoring steps
            // void WriteLogFailure(ProblemDetails problemDetails)
            //     => _logger.LogBookingByAccountFailure($"Failed to book using account. Reference code: '{referenceCode}'. Error: {problemDetails.Detail}");
            //
            //
            // Result<T, ProblemDetails> WriteLog<T>(Result<T, ProblemDetails> result)
            //     => LoggerUtils.WriteLogByResult(result,
            //         () => _logger.LogBookingFinalizationSuccess($"Successfully booked using account. Reference code: '{booking.ReferenceCode}'"),
            //         () => _logger.LogBookingFinalizationFailure(
            //             $"Failed to book using account. Reference code: '{booking.ReferenceCode}'. Error: {result.Error.Detail}"));
        }
        
        
        
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingAccountPaymentService _accountPaymentService;
        private readonly IBookingEvaluationStorage _bookingEvaluationStorage;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IBookingInfoService _bookingInfoService;
        private readonly IBookingRegistrationService _registrationService;
        private readonly IBookingRequestExecutor _requestExecutor;
    }
}