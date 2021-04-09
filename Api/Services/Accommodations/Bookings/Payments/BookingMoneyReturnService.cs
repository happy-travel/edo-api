using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments
{
    public class BookingMoneyReturnService : IBookingMoneyReturnService
    {
        public BookingMoneyReturnService(IBookingAccountPaymentService accountPaymentService,
            IBookingCreditCardPaymentService creditCardPaymentService)
        {
            _accountPaymentService = accountPaymentService;
            _creditCardPaymentService = creditCardPaymentService;
        }
        
        
        public async Task<Result> ReturnMoney(Booking booking, DateTime operationDate, ApiCaller apiCaller)
        {
            return booking.PaymentMethod switch
            {
                PaymentMethods.BankTransfer => await ReturnBankTransfer(),
                PaymentMethods.CreditCard => await ReturnCreditCardPayment(),
                PaymentMethods.Offline => Result.Success(),
                _ => throw new ArgumentOutOfRangeException(nameof(booking.PaymentMethod), $"Invalid payment method {booking.PaymentMethod}")
            };


            async Task<Result> ReturnBankTransfer()
                => booking.PaymentStatus switch
                {
                    BookingPaymentStatuses.NotPaid => Result.Success(),
                    BookingPaymentStatuses.Refunded => Result.Success(),
                    BookingPaymentStatuses.Captured => await _accountPaymentService.Refund(booking, operationDate, apiCaller),
                    _ => throw new ArgumentOutOfRangeException(nameof(booking.PaymentStatus), $"Invalid payment status {booking.PaymentStatus}")
                };


            async Task<Result> ReturnCreditCardPayment()
                => booking.PaymentStatus switch
                {
                    BookingPaymentStatuses.NotPaid => Result.Success(),
                    BookingPaymentStatuses.Refunded => Result.Success(),
                    BookingPaymentStatuses.Voided => Result.Success(),
                    BookingPaymentStatuses.Authorized => await _creditCardPaymentService.Void(booking, apiCaller),
                    BookingPaymentStatuses.Captured => await _creditCardPaymentService.Refund(booking, operationDate, apiCaller),
                    _ => throw new ArgumentOutOfRangeException(nameof(booking.PaymentStatus), $"Invalid payment status {booking.PaymentStatus}")
                };
        }
        
        
        private readonly IBookingAccountPaymentService _accountPaymentService;
        private readonly IBookingCreditCardPaymentService _creditCardPaymentService;
    }
}