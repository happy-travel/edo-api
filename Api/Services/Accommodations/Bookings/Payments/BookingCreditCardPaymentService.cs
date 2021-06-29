using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Payments.CreditCards;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public class BookingCreditCardPaymentService : IBookingCreditCardPaymentService
    {
        public BookingCreditCardPaymentService(ICreditCardPaymentProcessingService creditCardPaymentProcessingService,
            ILogger<BookingCreditCardPaymentService> logger,
            IDateTimeProvider dateTimeProvider,
            IBookingInfoService bookingInfoService,
            IBookingNotificationService bookingNotificationService,
            IBookingPaymentCallbackService paymentCallbackService)
        {
            _creditCardPaymentProcessingService = creditCardPaymentProcessingService;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
            _bookingInfoService = bookingInfoService;
            _bookingNotificationService = bookingNotificationService;
            _paymentCallbackService = paymentCallbackService;
        }
        

        public async Task<Result<string>> Capture(Booking booking, ApiCaller apiCaller)
        {
            if (booking.PaymentType != PaymentTypes.CreditCard)
            {
                _logger.LogCaptureMoneyForBookingFailure(booking.ReferenceCode, booking.PaymentType);
                return Result.Failure<string>($"Invalid payment method: {booking.PaymentType}");
            }

            _logger.LogCaptureMoneyForBookingSuccess(booking.ReferenceCode);
            return await _creditCardPaymentProcessingService.CaptureMoney(booking.ReferenceCode, apiCaller, _paymentCallbackService);
        }
        
        
        public async Task<Result> Void(Booking booking, ApiCaller apiCaller)
        {
            if (booking.PaymentStatus != BookingPaymentStatuses.Authorized)
                return Result.Failure($"Void is only available for payments with '{BookingPaymentStatuses.Authorized}' status");

            return await _creditCardPaymentProcessingService.VoidMoney(booking.ReferenceCode, apiCaller, _paymentCallbackService);
        }


        public async Task<Result> Refund(Booking booking, DateTime operationDate, ApiCaller apiCaller)
        {
            if (booking.PaymentStatus != BookingPaymentStatuses.Captured)
                return Result.Failure($"Refund is only available for payments with '{BookingPaymentStatuses.Captured}' status");
            
            return await _creditCardPaymentProcessingService.RefundMoney(booking.ReferenceCode, apiCaller, operationDate, _paymentCallbackService);
        }


        public async Task<Result> PayForAccountBooking(string referenceCode, AgentContext agent)
        {
            return await GetBooking(referenceCode, agent)
                .Ensure(IsBookingPaid, "Failed to pay for booking")
                .CheckIf(IsDeadlinePassed, CaptureMoney)
                .Tap(NotifyPaymentReceived);


            Task<Result<Booking>> GetBooking(string code, AgentContext agentContext) 
                => _bookingInfoService.GetAgentsBooking(code, agentContext);


            bool IsBookingPaid(Booking booking) 
                => booking.PaymentStatus == BookingPaymentStatuses.Authorized;
            
            
            bool IsDeadlinePassed(Booking booking) 
                => booking.GetPayDueDate() <= _dateTimeProvider.UtcToday();
            

            async Task<Result> CaptureMoney(Booking booking) 
                => await Capture(booking, agent.ToApiCaller());
            
            
            async Task NotifyPaymentReceived(Booking booking) 
                => await _bookingNotificationService.NotifyCreditCardPaymentConfirmed(booking.ReferenceCode);
        }
        
        
        private readonly ICreditCardPaymentProcessingService _creditCardPaymentProcessingService;
        private readonly ILogger<BookingCreditCardPaymentService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingInfoService _bookingInfoService;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly IBookingPaymentCallbackService _paymentCallbackService;
    }
}