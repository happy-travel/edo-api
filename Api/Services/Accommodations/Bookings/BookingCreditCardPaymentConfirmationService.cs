using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Management;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingCreditCardPaymentConfirmationService : IBookingCreditCardPaymentConfirmationService
    {
        public BookingCreditCardPaymentConfirmationService(
            IBookingRecordsManager bookingRecordsManager,
            IAgentContextService agentContext,
            IBookingRegistrationService bookingRegistrationService,
            IBookingResponseProcessor bookingResponseProcessor,
            IManagementAuditService managementAuditService)
        {
            _bookingRecordsManager = bookingRecordsManager;
            _agentContext = agentContext;
            _bookingRegistrationService = bookingRegistrationService;
            _bookingResponseProcessor = bookingResponseProcessor;
            _managementAuditService = _managementAuditService;
        }

        public async Task<Result<Booking, ProblemDetails>> Confirm(int bookingId, Administrator administrator)
        {
            var (_, isGetBookingFailure, booking, getBookingError) = await GetBooking()
                .Bind(CheckBookingCanBeConfirmed);

            if (isGetBookingFailure)
                return ProblemDetailsBuilder.Fail<Booking>(getBookingError);

            var agent = await _agentContext.GetAgent(booking.AgentId);

            return await _bookingRegistrationService.BookOnProvider(booking, booking.ReferenceCode, booking.LanguageCode)
                .Tap(ProcessResponse)
                .OnFailure(VoidMoneyAndCancelBooking)
                .Bind(SendReceipt)
                .Bind(GetAccommodationBookingInfo)
                .Bind(WriteAuditLog);

            async Task<Result<Booking>> GetBooking()
            {
                var (_, isFailure, bookingRecord, _) = await _bookingRecordsManager.Get(bookingId);
                return isFailure
                    ? Result.Failure<Booking>($"Could not find booking with id {bookingId}")
                    : Result.Success(bookingRecord);
            }

            Result<Booking> CheckBookingCanBeConfirmed(Booking booking)
                => booking.PaymentMethod == PaymentMethods.CreditCard
                    ? Result.Success(booking)
                    : Result.Failure<Booking>($"Could not complete booking. Invalid payment status: {booking.PaymentMethod}");

            Task ProcessResponse(EdoContracts.Accommodations.Booking bookingResponse) => _bookingResponseProcessor.ProcessResponse(bookingResponse, booking);


            Task VoidMoneyAndCancelBooking(ProblemDetails problemDetails) => _bookingRegistrationService.VoidMoneyAndCancelBooking(booking, agent);


            Task<Result<AccommodationBookingInfo, ProblemDetails>> GetAccommodationBookingInfo(EdoContracts.Accommodations.Booking details)
                => _bookingRecordsManager.GetAccommodationBookingInfo(details.ReferenceCode, booking.LanguageCode)
                    .ToResultWithProblemDetails();

            Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> SendReceipt(EdoContracts.Accommodations.Booking details) => _bookingRegistrationService.SendReceipt(details, booking, agent);


            async Task<Result<Booking, ProblemDetails>> WriteAuditLog(AccommodationBookingInfo details)
            {
                await _managementAuditService.Write(ManagementEventType.CreditCardPaymentConfirmation,
                    new CreditCardPaymentConfirmationEvent(administrator.Id, booking.ReferenceCode));
                return Result.Success<Booking, ProblemDetails>(booking);
            }
        }


        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IAgentContextService _agentContext;
        private readonly IBookingRegistrationService _bookingRegistrationService;
        private readonly IBookingResponseProcessor _bookingResponseProcessor;
        private readonly IManagementAuditService _managementAuditService;
    }
}