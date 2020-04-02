using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Api.Services.Payments.CreditCards;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingPaymentService : IBookingPaymentService
    {
        public BookingPaymentService(IDateTimeProvider dateTimeProvider, 
            IAdministratorContext adminContext, 
            EdoContext context,
            ILogger<PaymentService> logger,
            IAccountPaymentService accountPaymentService,
            ICreditCardPaymentService creditCardPaymentService,
            IPaymentNotificationService notificationService,
            IServiceAccountContext serviceAccountContext)
        {
            _dateTimeProvider = dateTimeProvider;
            _adminContext = adminContext;
            _context = context;
            _logger = logger;
            _accountPaymentService = accountPaymentService;
            _creditCardPaymentService = creditCardPaymentService;
            _notificationService = notificationService;
            _serviceAccountContext = serviceAccountContext;
        }
        
        public async Task<Result<List<int>>> GetBookingsForCapture(DateTime deadlineDate)
        {
            if (deadlineDate == default)
                return Result.Fail<List<int>>("Deadline date should be specified");

            var (_, isFailure, _, error) = await _serviceAccountContext.GetUserInfo();
            if (isFailure)
                return Result.Fail<List<int>>(error);

            var bookings = await _context.Bookings
                .Where(booking =>
                    BookingStatusesForPayment.Contains(booking.Status) && booking.PaymentStatus == BookingPaymentStatuses.Authorized &&
                    (booking.PaymentMethod == PaymentMethods.BankTransfer || booking.PaymentMethod == PaymentMethods.CreditCard))
                .ToListAsync();

            var date = deadlineDate.Date;
            var bookingIds = bookings
                .Where(IsTimeToCaptureMoney)
                .Select(booking => booking.Id)
                .ToList();

            return Result.Ok(bookingIds);


            bool IsTimeToCaptureMoney(Booking booking)
            {
                var availabilityInfo = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);
                if (availabilityInfo.CheckInDate <= date)
                    return true;
                
                return availabilityInfo.Agreement.RoomContracts.All(rc=> rc.DeadlineDate != null) && availabilityInfo.Agreement.RoomContracts.All(rc => rc.DeadlineDate.Value.Date < date);
            }
        }


        public async Task<Result<ProcessResult>> CaptureMoneyForBookings(List<int> bookingIds)
        {
            var (_, isUserFailure, _, userError) = await _serviceAccountContext.GetUserInfo();
            if (isUserFailure)
                return Result.Fail<ProcessResult>(userError);

            var bookings = await GetBookings();

            return await Validate()
                .OnSuccess(ProcessBookings);


            Task<List<Booking>> GetBookings()
            {
                var ids = bookingIds;
                return _context.Bookings.Where(booking => ids.Contains(booking.Id)).ToListAsync();
            }


            Result Validate()
            {
                return bookings.Count != bookingIds.Count
                    ? Result.Fail("Invalid booking ids. Could not find some of requested bookings.")
                    : Result.Combine(bookings.Select(ValidateBooking).ToArray());


                Result ValidateBooking(Booking booking)
                    => GenericValidator<Booking>.Validate(v =>
                    {
                        v.RuleFor(c => c.PaymentStatus)
                            .Must(status => booking.PaymentStatus == BookingPaymentStatuses.Authorized)
                            .WithMessage(
                                $"Invalid payment status for the booking '{booking.ReferenceCode}': {booking.PaymentStatus}");
                        v.RuleFor(c => c.Status)
                            .Must(status => BookingStatusesForPayment.Contains(status))
                            .WithMessage($"Invalid booking status for the booking '{booking.ReferenceCode}': {booking.Status}");
                        v.RuleFor(c => c.PaymentMethod)
                            .Must(method => method == PaymentMethods.BankTransfer ||
                                method == PaymentMethods.CreditCard)
                            .WithMessage($"Invalid payment method for the booking '{booking.ReferenceCode}': {booking.PaymentMethod}");
                    }, booking);
            }


            Task<ProcessResult> ProcessBookings()
            {
                return Combine(bookings.Select(ProcessBooking));


                Task<Result<string>> ProcessBooking(Booking booking)
                {
                    switch (booking.PaymentMethod)
                    {
                        case PaymentMethods.BankTransfer:
                            return _accountPaymentService.CaptureMoney(booking);
                        case PaymentMethods.CreditCard:
                            return _creditCardPaymentService.CaptureMoney(booking);
                        default: return Task.FromResult(Result.Fail<string>($"Invalid payment method: {booking.PaymentMethod}"));
                    }
                }
            }
        }


        public Task<Result> VoidMoney(Booking booking)
        {
            // TODO: Add logging
            // TODO: Implement refund money if status is paid with deadline penalty
            if (booking.PaymentStatus != BookingPaymentStatuses.Authorized)
                return Task.FromResult(Result.Ok());

            switch (booking.PaymentMethod)
            {
                case PaymentMethods.BankTransfer:
                    return _accountPaymentService.VoidMoney(booking);
                case PaymentMethods.CreditCard:
                    return _creditCardPaymentService.VoidMoney(booking);
                default: return Task.FromResult(Result.Fail($"Could not void money for the booking with a payment method '{booking.PaymentMethod}'"));
            }
        }


        public async Task<Result> CompleteOffline(int bookingId)
        {
            return await _adminContext.GetCurrent()
                .OnSuccess(GetBooking)
                .OnSuccess(CheckBookingCanBeCompleted)
                .OnSuccess(Complete)
                .OnSuccess(SendBillToCustomer);


            async Task<Result<Booking>> GetBooking()
            {
                var entity = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
                return entity == null
                    ? Result.Fail<Booking>($"Could not find booking with id {bookingId}")
                    : Result.Ok(entity);
            }


            Result<Booking> CheckBookingCanBeCompleted(Booking booking)
                => booking.PaymentStatus == BookingPaymentStatuses.NotPaid
                    ? Result.Ok(booking)
                    : Result.Fail<Booking>($"Could not complete booking. Invalid payment status: {booking.PaymentStatus}");


            Task Complete(Booking booking)
            {
                booking.PaymentMethod = PaymentMethods.Offline;
                return ChangeBookingPaymentStatusToCaptured(booking);
            }


            async Task SendBillToCustomer(Booking booking)
            {
                var availabilityInfo = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

                var currency = availabilityInfo.Agreement.Price.Currency;

                var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == booking.CustomerId);
                if (customer == default)
                {
                    _logger.LogWarning("Send bill after offline payment: could not find customer with id '{0}' for the booking '{1}'", booking.CustomerId,
                        booking.ReferenceCode);
                    return;
                }

                await _notificationService.SendBillToCustomer(new PaymentBill(customer.Email,
                    availabilityInfo.Agreement.Price.NetTotal,
                    currency,
                    _dateTimeProvider.UtcNow(),
                    PaymentMethods.Offline,
                    booking.ReferenceCode,
                    $"{customer.LastName} {customer.FirstName}"));
            }
        }


        public async Task<Result<ProcessResult>> NotifyPaymentsNeeded(List<int> bookingIds)
        {
            var (_, isUserFailure, _, userError) = await _serviceAccountContext.GetUserInfo();
            if (isUserFailure)
                return Result.Fail<ProcessResult>(userError);

            var bookings = await GetBookings();

            return await Validate()
                .OnSuccess(ProcessBookings);

            Task<List<Booking>> GetBookings() => _context.Bookings.Where(booking => bookingIds.Contains(booking.Id)).ToListAsync();


            Result Validate()
            {
                return bookings.Count != bookingIds.Count
                    ? Result.Fail("Invalid booking ids. Could not find some of requested bookings.")
                    : Result.Combine(bookings.Select(ValidateBooking).ToArray());


                Result ValidateBooking(Booking booking)
                    => GenericValidator<Booking>.Validate(v =>
                    {
                        v.RuleFor(c => c.PaymentStatus)
                            .Must(status => booking.PaymentStatus == BookingPaymentStatuses.NotPaid
                                || booking.PaymentStatus == BookingPaymentStatuses.PartiallyAuthorized)
                            .WithMessage(
                                $"Invalid payment status for the booking '{booking.ReferenceCode}': {booking.PaymentStatus}");
                        v.RuleFor(c => c.Status)
                            .Must(status => BookingStatusesForPayment.Contains(status))
                            .WithMessage($"Invalid booking status for the booking '{booking.ReferenceCode}': {booking.Status}");
                        v.RuleFor(c => c.PaymentMethod)
                            .Must(method => method == PaymentMethods.BankTransfer ||
                                method == PaymentMethods.CreditCard)
                            .WithMessage($"Invalid payment method for the booking '{booking.ReferenceCode}': {booking.PaymentMethod}");
                    }, booking);
            }


            Task<ProcessResult> ProcessBookings()
            {
                return Combine(bookings.Select(ProcessBooking));


                Task<Result<string>> ProcessBooking(Booking booking)
                {
                    var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

                    var currency = bookingAvailability.Agreement.Price.Currency;

                    return Notify()
                        .OnBoth(CreateResult);


                    async Task<Result> Notify()
                    {
                        var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == booking.CustomerId);
                        if (customer == default)
                            return Result.Fail($"Could not find customer with id {booking.CustomerId}");

                        return await _notificationService.SendNeedPaymentNotificationToCustomer(new PaymentBill(customer.Email,
                            bookingAvailability.Agreement.Price.NetTotal,
                            currency,
                            DateTime.MinValue,
                            booking.PaymentMethod,
                            booking.ReferenceCode,
                            $"{customer.LastName} {customer.FirstName}"));
                    }


                    Result<string> CreateResult(Result result)
                        => result.IsSuccess
                            ? Result.Ok($"Payment for the booking '{booking.ReferenceCode}' completed.")
                            : Result.Fail<string>($"Unable to complete payment for the booking '{booking.ReferenceCode}'. Reason: {result.Error}");
                }
            }
        }


        public async Task<Result<Price>> GetPendingAmount(int bookingId)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
            if (booking == default)
                return Result.Fail<Price>($"Could not find booking with id {bookingId}");

            return await GetPendingAmount(booking);
        }


        private async Task<Result<Price>> GetPendingAmount(Booking booking)
        {
            switch (booking.PaymentMethod)
            {
                case PaymentMethods.CreditCard:
                    return await _creditCardPaymentService.GetPendingAmount(booking);
                case PaymentMethods.BankTransfer:
                    return await _accountPaymentService.GetPendingAmount(booking);
                default:
                    return Result.Fail<Price>($"Unsupported payment method for pending payment: {booking.PaymentMethod}");
            }
        }


        private async Task<ProcessResult> Combine(IEnumerable<Task<Result<string>>> results)
        {
            var builder = new StringBuilder();

            foreach (var result in results)
            {
                var (_, isFailure, value, error) = await result;
                builder.AppendLine(isFailure ? error : value);
            }

            return new ProcessResult(builder.ToString());
        }


        private Task ChangeBookingPaymentStatusToCaptured(Booking booking)
        {
            booking.PaymentStatus = BookingPaymentStatuses.Captured;
            _context.Bookings.Update(booking);
            return _context.SaveChangesAsync();
        }
        
        private static readonly HashSet<BookingStatusCodes> BookingStatusesForPayment = new HashSet<BookingStatusCodes>
        {
            BookingStatusCodes.Pending, BookingStatusCodes.Confirmed
        };

        private readonly IAdministratorContext _adminContext;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<PaymentService> _logger;
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly ICreditCardPaymentService _creditCardPaymentService;
        private readonly IPaymentNotificationService _notificationService;
        private readonly IServiceAccountContext _serviceAccountContext;
    }
}