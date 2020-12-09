using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Api.Services.Payments.CreditCards;
using HappyTravel.Edo.Api.Services.Payments.Offline;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Management;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingPaymentService : IBookingPaymentService
    {
        public BookingPaymentService(EdoContext context,
            IAccountPaymentService accountPaymentService,
            ICreditCardPaymentProcessingService creditCardPaymentProcessingService,
            IBookingRecordsManager recordsManager,
            IBookingDocumentsService documentsService,
            IPaymentNotificationService notificationService,
            ILogger<BookingPaymentService> logger,
            IOfflinePaymentAuditService offlinePaymentAuditService)
        {
            _context = context;
            _accountPaymentService = accountPaymentService;
            _creditCardPaymentProcessingService = creditCardPaymentProcessingService;
            _recordsManager = recordsManager;
            _documentsService = documentsService;
            _notificationService = notificationService;
            _logger = logger;
            _offlinePaymentAuditService = offlinePaymentAuditService;
        }


        public async Task<Result<string>> Capture(Booking booking, UserInfo user)
        {
            if (booking.PaymentMethod != PaymentMethods.CreditCard)
            {
                _logger.LogCaptureMoneyForBookingFailure($"Failed to capture money for a booking with reference code: '{booking.ReferenceCode}'. " +
                    $"Error: Invalid payment method: {booking.PaymentMethod}");
                return Result.Failure<string>($"Invalid payment method: {booking.PaymentMethod}");
            }

            _logger.LogCaptureMoneyForBookingSuccess($"Successfully captured money for a booking with reference code: '{booking.ReferenceCode}'");
            return await _creditCardPaymentProcessingService.CaptureMoney(booking.ReferenceCode, user, this);


        }


        public async Task<Result<string>> Charge(Booking booking, UserInfo user)
        {
            return await CheckPaymentMethod()
                .Bind(Charge)
                .Bind(SendReceipt)
                .Finally(WriteLog);


            Result CheckPaymentMethod() => 
                booking.PaymentMethod == PaymentMethods.BankTransfer
                    ? Result.Success()
                    : Result.Failure($"Failed to charge money for a booking with reference code: '{booking.ReferenceCode}'. " +
                        $"Error: Invalid payment method: {booking.PaymentMethod}");
            

            async Task<Result<string>> Charge()
            {
                var (_, isFailure, _, error) = await _accountPaymentService.Charge(booking, user, booking.AgencyId, null);
                if (isFailure)
                    return Result.Failure<string>($"Unable to charge payment for a booking with reference code: '{booking.ReferenceCode}'. " +
                        $"Error while charging: {error}");

                return Result.Success($"Successfully charged money for a booking with reference code: '{booking.ReferenceCode}'");
            }


            async Task<Result<string>> SendReceipt(string chargeMessage)
            {
                var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == booking.AgentId);
                var (_, isFailure, receiptInfo, error) = await _documentsService.GenerateReceipt(booking.Id, booking.AgentId);

                if (isFailure)
                    return Result.Failure<string>($"Unable to charge payment for a booking with reference code: '{booking.ReferenceCode}'. " +
                        $"Error while sending receipt: {error}");

                await _notificationService.SendReceiptToCustomer(receiptInfo, agent.Email);
                return chargeMessage;
            }


            Result<string> WriteLog(Result<string> result)
            {
                if (result.IsSuccess)
                    _logger.LogChargeMoneyForBookingSuccess(result.Value);
                else
                    _logger.LogChargeMoneyForBookingFailure(result.Error);

                return result;
            }
        }


        public Task<Result> VoidOrRefund(Booking booking, UserInfo user)
        {
            // TODO: Add logging

            switch (booking.PaymentMethod)
            {
                case PaymentMethods.BankTransfer:
                    return RefundBankTransfer();
                case PaymentMethods.CreditCard:
                    return VoidOrRefundCard();
                default: 
                    return Task.FromResult(Result.Failure($"Could not void money for the booking with a payment method '{booking.PaymentMethod}'"));
            }


            Task<Result> VoidOrRefundCard()
            {
                if (booking.PaymentStatus == BookingPaymentStatuses.Captured)
                    return _creditCardPaymentProcessingService.RefundMoney(booking.ReferenceCode, user, this);

                if (booking.PaymentStatus != BookingPaymentStatuses.Authorized)
                    return Task.FromResult(Result.Success());

                return _creditCardPaymentProcessingService.VoidMoney(booking.ReferenceCode, user, this);
            }


            Task<Result> RefundBankTransfer()
            {
                if (booking.PaymentStatus != BookingPaymentStatuses.Captured)
                    return Task.FromResult(Result.Success());

                return _accountPaymentService.Refund(booking, user);
            }
        }


        public async Task<Result> CompleteOffline(int bookingId, Administrator administratorContext)
        {
            // TODO: Add admin actions audit log NIJO-659
            return await GetBooking()
                .Bind(CheckBookingCanBeCompleted)
                .Tap(Complete)
                .Tap(WriteAuditLog);


            async Task<Result<Booking>> GetBooking()
            {
                var (_, isFailure, booking, _) = await _recordsManager.Get(bookingId);
                return isFailure
                    ? Result.Failure<Booking>($"Could not find booking with id {bookingId}")
                    : Result.Success(booking);
            }


            Result<Booking> CheckBookingCanBeCompleted(Booking booking)
                => booking.PaymentStatus == BookingPaymentStatuses.NotPaid
                    ? Result.Success(booking)
                    : Result.Failure<Booking>($"Could not complete booking. Invalid payment status: {booking.PaymentStatus}");


            Task Complete(Booking booking)
            {
                booking.PaymentMethod = PaymentMethods.Offline;
                return ChangeBookingPaymentStatusToCaptured(booking);
            }


            Task WriteAuditLog(Booking booking) => _offlinePaymentAuditService.Write(administratorContext.ToUserInfo(), booking.ReferenceCode);
        }


        private Task ChangeBookingPaymentStatusToCaptured(Booking booking)
        {
            booking.PaymentStatus = BookingPaymentStatuses.Captured;
            _context.Bookings.Update(booking);
            return _context.SaveChangesAsync();
        }
        
        
        
        public async Task<Result<MoneyAmount>> GetServicePrice(string referenceCode)
        {
            var booking = await _context.Bookings.SingleOrDefaultAsync(b => b.ReferenceCode == referenceCode);
            if (booking == default)
                return Result.Failure<MoneyAmount>("Could not find booking");

            return Result.Success(new MoneyAmount(booking.TotalPrice, booking.Currency));
        }


        public async Task<Result> ProcessPaymentChanges(Payment payment)
        {
            var booking = await _context.Bookings.SingleOrDefaultAsync(b => b.ReferenceCode == payment.ReferenceCode);
            if (booking == default)
            {
                _logger.LogProcessPaymentChangesForBookingFailure("Failed to process payment changes, " +
                    $"could not find the corresponding booking. Payment status: {payment.Status}. Payment: '{payment.ReferenceCode}'");

                return Result.Failure($"Could not find booking for payment '{payment.ReferenceCode}'");
            }

            var oldPaymentStatus = booking.PaymentStatus;

            switch (payment.Status)
            {
                case PaymentStatuses.Authorized:
                    booking.PaymentStatus = BookingPaymentStatuses.Authorized;
                    break;
                case PaymentStatuses.Captured:
                    booking.PaymentStatus = BookingPaymentStatuses.Captured;
                    break;
                case PaymentStatuses.Voided:
                    booking.PaymentStatus = BookingPaymentStatuses.Voided;
                    break;
                case PaymentStatuses.Refunded:
                    booking.PaymentStatus = BookingPaymentStatuses.Refunded;
                    break;
                default: 
                    _logger.LogProcessPaymentChangesForBookingSkip("Skipped booking status update while processing payment changes. " +
                        $"Payment status: {payment.Status}. Payment: '{payment.ReferenceCode}'. Booking reference code: '{booking.ReferenceCode}'");

                    return Result.Success();
            }

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            
            _context.Entry(booking).State = EntityState.Detached;

            _logger.LogProcessPaymentChangesForBookingSuccess($"Successfully processes payment changes. Old payment status: {oldPaymentStatus}. " +
                $"New payment status: {payment.Status}. Payment: '{payment.ReferenceCode}'. Booking reference code: '{booking.ReferenceCode}'");

            return Result.Success();
        }


        public async Task<Result<(int AgentId, int AgencyId)>> GetServiceBuyer(string referenceCode)
        {
            var (_, isFailure, booking, error) = await _recordsManager.Get(referenceCode);
            if (isFailure)
                return Result.Failure<(int, int)>(error);

            return (booking.AgentId, booking.AgencyId);
        }


        private readonly EdoContext _context;
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly ICreditCardPaymentProcessingService _creditCardPaymentProcessingService;
        private readonly IBookingRecordsManager _recordsManager;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly ILogger<BookingPaymentService> _logger;
        private readonly IOfflinePaymentAuditService _offlinePaymentAuditService;
    }
}