using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingsProcessingService : IBookingsProcessingService
    {
        public BookingsProcessingService(IServiceAccountContext serviceAccountContext,
            IDateTimeProvider dateTimeProvider,
            EdoContext context,
            IBookingService bookingService)
        {
            _serviceAccountContext = serviceAccountContext;
            _dateTimeProvider = dateTimeProvider;
            _context = context;
            _bookingService = bookingService;
        }


        public async Task<Result<List<int>>> GetForCancellation(DateTime deadlineDate)
        {
            if (deadlineDate == default)
                return Result.Fail<List<int>>("Deadline date should be specified");

            var (_, isFailure, _, error) = await _serviceAccountContext.GetUserInfo();
            if (isFailure)
                return Result.Fail<List<int>>(error);

            // It’s prohibited to cancel booking after check-in date
            var currentDateUtc = _dateTimeProvider.UtcNow();
            var dayBeforeDeadline = deadlineDate.Date.AddDays(1);
            var bookingIds = await _context.Bookings
                .Where(booking =>
                    BookingStatusesForCancellation.Contains(booking.Status) &&
                    PaymentStatusesForCancellation.Contains(booking.PaymentStatus) &&
                    booking.BookingDate > currentDateUtc &&
                    booking.DeadlineDate != null &&
                    booking.DeadlineDate.Value <= dayBeforeDeadline
                )
                .Select(booking => booking.Id)
                .ToListAsync();

            return Result.Ok(bookingIds);
        }


        public async Task<Result<ProcessResult>> Cancel(List<int> bookingIds, RequestMetadata requestMetadata)
        {
            var (_, isUserFailure, _, userError) = await _serviceAccountContext.GetUserInfo();
            if (isUserFailure)
                return Result.Fail<ProcessResult>(userError);

            var bookings = await GetBookings();

            return await Validate()
                .OnSuccess(ProcessBookings);


            Task<List<Data.Booking.Booking>> GetBookings()
            {
                var ids = bookingIds;
                return _context.Bookings.Where(booking => ids.Contains(booking.Id)).ToListAsync();
            }


            Result Validate()
            {
                return bookings.Count != bookingIds.Count
                    ? Result.Fail("Invalid booking ids. Could not find some of requested bookings.")
                    : Result.Combine(bookings.Select(CheckCanBeCancelled).ToArray());


                Result CheckCanBeCancelled(Data.Booking.Booking booking)
                    => GenericValidator<Data.Booking.Booking>.Validate(v =>
                    {
                        v.RuleFor(c => c.PaymentStatus)
                            .Must(status => PaymentStatusesForCancellation.Contains(booking.PaymentStatus))
                            .WithMessage(
                                $"Invalid payment status for the booking '{booking.ReferenceCode}': {booking.PaymentStatus}");
                        v.RuleFor(c => c.Status)
                            .Must(status => BookingStatusesForCancellation.Contains(status))
                            .WithMessage($"Invalid booking status for the booking '{booking.ReferenceCode}': {booking.Status}");
                    }, booking);
            }


            Task<ProcessResult> ProcessBookings()
            {
                return Combine(bookings.Select(ProcessBooking));


                Task<Result<string>> ProcessBooking(Data.Booking.Booking booking)
                {
                    return _bookingService.Cancel(booking.Id, requestMetadata)
                        .OnBoth(CreateResult);


                    Result<string> CreateResult(Result<VoidObject, ProblemDetails> result)
                        => result.IsSuccess
                            ? Result.Ok($"Booking '{booking.ReferenceCode}' was cancelled.")
                            : Result.Fail<string>($"Unable to cancel booking '{booking.ReferenceCode}'. Reason: {result.Error.Detail}");
                }


                async Task<ProcessResult> Combine(IEnumerable<Task<Result<string>>> results)
                {
                    var builder = new StringBuilder();

                    foreach (var result in results)
                    {
                        var (_, isFailure, value, error) = await result;
                        builder.AppendLine(isFailure ? error : value);
                    }

                    return new ProcessResult(builder.ToString());
                }
            }
        }


        private static readonly HashSet<BookingStatusCodes> BookingStatusesForCancellation = new HashSet<BookingStatusCodes>
        {
            BookingStatusCodes.Pending, BookingStatusCodes.Confirmed
        };


        private static readonly HashSet<BookingPaymentStatuses> PaymentStatusesForCancellation = new HashSet<BookingPaymentStatuses>
        {
            BookingPaymentStatuses.NotPaid, BookingPaymentStatuses.Authorized
        };

        private readonly IServiceAccountContext _serviceAccountContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly EdoContext _context;
        private readonly IBookingService _bookingService;
    }
}