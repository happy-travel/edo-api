using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments.CreditCardConfirmation;
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
            ICreditCardPaymentConfirmationAuditService creditCardPaymentConfirmationAuditService,
            IAgentContextService agentContext,
            IBookingRegistrationService bookingRegistrationService,
            IBookingResponseProcessor bookingResponseProcessor)
        {
            _bookingRecordsManager = bookingRecordsManager;
            _creditCardPaymentConfirmationAuditService = creditCardPaymentConfirmationAuditService;
            _agentContext = agentContext;
            _bookingRegistrationService = bookingRegistrationService;
            _bookingResponseProcessor = bookingResponseProcessor;
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
                await _creditCardPaymentConfirmationAuditService.Write(administrator.ToUserInfo(), booking.ReferenceCode);
                return Result.Success<Booking, ProblemDetails>(booking);
            }
        }


        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly ICreditCardPaymentConfirmationAuditService _creditCardPaymentConfirmationAuditService;
        private readonly IAgentContextService _agentContext;
        private readonly IBookingRegistrationService _bookingRegistrationService;
        private readonly IBookingResponseProcessor _bookingResponseProcessor;
    }
}