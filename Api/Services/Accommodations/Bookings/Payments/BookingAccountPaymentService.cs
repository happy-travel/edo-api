using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public class BookingAccountPaymentService : IBookingAccountPaymentService
    {
        public BookingAccountPaymentService(IAccountPaymentService accountPaymentService,
            IBookingDocumentsService documentsService,
            IBookingPaymentCallbackService paymentCallbackService,
            ILogger<BookingAccountPaymentService> logger,
            EdoContext context,
            IBookingDocumentsMailingService documentsMailingService)
        {
            _accountPaymentService = accountPaymentService;
            _documentsService = documentsService;
            _paymentCallbackService = paymentCallbackService;
            _logger = logger;
            _context = context;
            _documentsMailingService = documentsMailingService;
        }


        public async Task<Result> Refund(Booking booking, DateTime operationDate, ApiCaller apiCaller)
        {
            if (booking.PaymentStatus != BookingPaymentStatuses.Captured)
                return Result.Success();

            var reason = $"Refunding money for booking {booking.ReferenceCode}";
            return await _accountPaymentService.Refund(booking.ReferenceCode, apiCaller, operationDate, _paymentCallbackService, reason);
        }

        
        public async Task<Result<string>> Charge(Booking booking, ApiCaller apiCaller)
        {
            return await CheckPaymentMethod()
                .Bind(Charge)
                .Bind(SendReceipt)
                .Finally(WriteLog);


            Result CheckPaymentMethod()
                => booking.PaymentMethod == PaymentTypes.VirtualAccount
                    ? Result.Success()
                    : Result.Failure($"Failed to charge money for a booking with reference code: '{booking.ReferenceCode}'. " +
                        $"Error: Invalid payment method: {booking.PaymentMethod}");


            async Task<Result<string>> Charge()
            {
                var (_, isFailure, _, error) = await _accountPaymentService.Charge(booking.ReferenceCode, apiCaller, _paymentCallbackService);
                if (isFailure)
                    return Result.Failure<string>($"Unable to charge payment for a booking with reference code: '{booking.ReferenceCode}'. " +
                        $"Error while charging: {error}");

                return Result.Success($"Successfully charged money for a booking with reference code: '{booking.ReferenceCode}'");
            }


            async Task<Result<string>> SendReceipt(string chargeMessage)
            {
                var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == booking.AgentId);
                var (_, isFailure, receiptInfo, error) = await _documentsService.GenerateReceipt(booking);

                if (isFailure)
                    return Result.Failure<string>($"Unable to charge payment for a booking with reference code: '{booking.ReferenceCode}'. " +
                        $"Error while sending receipt: {error}");

                await _documentsMailingService.SendReceiptToCustomer(receiptInfo, agent.Email, apiCaller);
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

        
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly IBookingDocumentsService _documentsService;
        private readonly IBookingPaymentCallbackService _paymentCallbackService;
        private readonly ILogger<BookingAccountPaymentService> _logger;
        private readonly EdoContext _context;
        private readonly IBookingDocumentsMailingService _documentsMailingService;
    }
}