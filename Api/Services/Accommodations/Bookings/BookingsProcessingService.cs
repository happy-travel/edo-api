using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Management;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingsProcessingService : IBookingsProcessingService
    {
        public BookingsProcessingService(IBookingPaymentService bookingPaymentService,
            IBookingManagementService bookingManagementService,
            IBookingMailingService bookingMailingService,
            EdoContext context)
        {
            _bookingPaymentService = bookingPaymentService;
            _bookingManagementService = bookingManagementService;
            _bookingMailingService = bookingMailingService;
            _context = context;
        }


        public Task<List<int>> GetForCapture(DateTime date)
        {
            date = date.Date;
            var daysBeforeDeadline = Infrastructure.Constants.Common.DaysBeforeDeadlineWhenPayForBooking;

            return _context.Bookings
                .Where(IsBookingValidForCapturePredicate)
                .Where(b => b.CheckInDate <= date.AddDays(daysBeforeDeadline) 
                    || (b.DeadlineDate.HasValue && b.DeadlineDate.Value.Date <= date.AddDays(daysBeforeDeadline)))
                .Select(b => b.Id)
                .ToListAsync();
        }


        public Task<Result<BatchOperationResult>> Capture(List<int> bookingIds, ServiceAccount serviceAccount)
        {
            return ExecuteBatchAction(bookingIds,
                IsBookingValidForCapturePredicate,
                Capture,
                serviceAccount);

            Task<Result<string>> Capture(Booking booking, UserInfo serviceAcc) => _bookingPaymentService.Capture(booking, serviceAccount.ToUserInfo());
        }


        public Task<List<int>> GetForCharge(DateTime date)
        {
            date = date.Date;
            var daysBeforeDeadline = Infrastructure.Constants.Common.DaysBeforeDeadlineWhenPayForBooking;

            return _context.Bookings
                .Where(IsBookingValidForChargePredicate)
                .Where(b => b.CheckInDate <= date.AddDays(daysBeforeDeadline) 
                    || (b.DeadlineDate.HasValue && b.DeadlineDate.Value.Date <= date.AddDays(daysBeforeDeadline)))
                .Select(b => b.Id)
                .ToListAsync();
        }


        public Task<Result<BatchOperationResult>> Charge(List<int> bookingIds, ServiceAccount serviceAccount)
        {
            return ExecuteBatchAction(bookingIds,
                IsBookingValidForChargePredicate,
                Charge,
                serviceAccount);


            async Task<Result<string>> Charge(Booking booking, UserInfo serviceAcc)
            {
                var chargeResult = await _bookingPaymentService.Charge(booking, serviceAccount.ToUserInfo());
                
                if (chargeResult.IsFailure)
                    await _bookingManagementService.Cancel(booking.Id, serviceAccount);

                return chargeResult;
            }
        }


        public Task<List<int>> GetForNotification(DateTime date)
        {
            date = date.Date.AddDays(DaysBeforeNotification);
            return _context.Bookings
                .Where(IsBookingValidForDeadlineNotification)
                .Where(b => b.CheckInDate == date || (b.DeadlineDate.HasValue && b.DeadlineDate.Value.Date == date))
                .Select(b => b.Id)
                .ToListAsync();
        }


        public Task<Result<BatchOperationResult>> NotifyDeadlineApproaching(List<int> bookingIds, ServiceAccount serviceAccount)
        {
            return ExecuteBatchAction(bookingIds,
                IsBookingValidForDeadlineNotification,
                Notify,
                serviceAccount);


            Task<Result<string>> Notify(Booking booking, UserInfo _)
            {
                return NotifyAgent()
                    .Finally(CreateResult);


                async Task<Result> NotifyAgent()
                {
                    var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == booking.AgentId);
                    if (agent == default)
                        return Result.Failure($"Could not find agent with id {booking.AgentId}");

                    return await _bookingMailingService.NotifyDeadlineApproaching(booking.Id, agent.Email);
                }


                Result<string> CreateResult(Result result)
                    => result.IsSuccess
                        ? Result.Success($"Notification for the booking '{booking.ReferenceCode}' was sent.")
                        : Result.Failure<string>($"Unable to notify agent for the booking '{booking.ReferenceCode}'. Reason: {result.Error}");
            }
        }


        public Task<List<int>> GetForCancellation()
        {
            return _context.Bookings
                .Where(IsBookingValidForCancelPredicate)
                .Select(booking => booking.Id)
                .ToListAsync();
        }


        public Task<Result<BatchOperationResult>> Cancel(List<int> bookingIds, ServiceAccount serviceAccount)
        {
            return ExecuteBatchAction(bookingIds,
                IsBookingValidForCancelPredicate,
                ProcessBooking,
                serviceAccount);


            Task<Result<string>> ProcessBooking(Booking booking, UserInfo _)
            {
                return _bookingManagementService.Cancel(booking.Id, serviceAccount)
                    .Finally(CreateResult);


                Result<string> CreateResult(Result<Unit, ProblemDetails> result)
                    => result.IsSuccess
                        ? Result.Success($"Booking '{booking.ReferenceCode}' was cancelled.")
                        : Result.Failure<string>($"Unable to cancel booking '{booking.ReferenceCode}'. Reason: {result.Error.Detail}");
            }
        }


        public async Task<BatchOperationResult> SendBookingSummaryReports()
        {
            var agencyIds = await _context.Agencies
                .Where(IsAgencyValidForBookingSummaryReportPredicate)
                .Select(agency => agency.Id)
                .ToListAsync();

            var builder = new StringBuilder();
            var hasErrors = false;

            foreach (var agencyId in agencyIds)
            {
                var (_, isFailure, message, error) = await _bookingMailingService.SendBookingReports(agencyId);
                if (isFailure)
                    hasErrors = true;

                builder.AppendLine(isFailure ? error : message);
            }

            return new BatchOperationResult(builder.ToString(), hasErrors);
        }


        private async Task<Result<BatchOperationResult>> ExecuteBatchAction(List<int> bookingIds,
            Expression<Func<Booking, bool>> predicate,
            Func<Booking, UserInfo, Task<Result<string>>> action,
            ServiceAccount serviceAccount)
        {
            var bookings = await GetBookings();

            return await ValidateCount()
                .Map(ProcessBookings);


            Task<List<Booking>> GetBookings()
                => _context.Bookings
                    .Where(booking => bookingIds.Contains(booking.Id))
                    .Where(predicate)
                    .ToListAsync();


            Result ValidateCount()
                => bookings.Count != bookingIds.Count
                    ? Result.Failure("Invalid booking ids. Could not find some of requested bookings.")
                    : Result.Success();


            Task<BatchOperationResult> ProcessBookings() => Combine(bookings.Select(booking => action(booking, serviceAccount.ToUserInfo())));


            async Task<BatchOperationResult> Combine(IEnumerable<Task<Result<string>>> results)
            {
                var builder = new StringBuilder();
                bool hasErrors = false;

                foreach (var result in results)
                {
                    var (_, isFailure, value, error) = await result;
                    if (isFailure)
                        hasErrors = true;
                    
                    builder.AppendLine(isFailure ? error : value);
                }

                return new BatchOperationResult(builder.ToString(), hasErrors);
            }
        }


        private const int DaysBeforeNotification = 3;


        private static readonly Expression<Func<Booking, bool>> IsBookingValidForCancelPredicate = booking
            => BookingStatusesForCancellation.Contains(booking.Status) && 
            PaymentStatusesForCancellation.Contains(booking.PaymentStatus) &&
            booking.PaymentMethod == PaymentMethods.CreditCard;
        
        private static readonly HashSet<BookingStatuses> BookingStatusesForCancellation = new HashSet<BookingStatuses>
        {
            BookingStatuses.Pending, BookingStatuses.Confirmed
        };

        private static readonly HashSet<BookingPaymentStatuses> PaymentStatusesForCancellation = new HashSet<BookingPaymentStatuses>
        {
            BookingPaymentStatuses.NotPaid, BookingPaymentStatuses.Refunded, BookingPaymentStatuses.Voided
        };

        private static readonly Expression<Func<Booking, bool>> IsBookingValidForCapturePredicate = booking
            => BookingStatusesForPayment.Contains(booking.Status) &&
            PaymentMethodsForCapture.Contains(booking.PaymentMethod) &&
            booking.PaymentStatus == BookingPaymentStatuses.Authorized;

        private static readonly Expression<Func<Booking, bool>> IsBookingValidForChargePredicate = booking
            => BookingStatusesForPayment.Contains(booking.Status) &&
            PaymentMethodsForCharge.Contains(booking.PaymentMethod) &&
            booking.PaymentStatus == BookingPaymentStatuses.NotPaid;

        private static readonly Expression<Func<Booking, bool>> IsBookingValidForDeadlineNotification = booking
            => BookingStatusesForPayment.Contains(booking.Status) &&
            PaymentStatusesForNotification.Contains(booking.PaymentStatus);

        private static readonly Expression<Func<Agency, bool>> IsAgencyValidForBookingSummaryReportPredicate = agency
            => agency.IsActive;

        private static readonly HashSet<BookingStatuses> BookingStatusesForPayment = new HashSet<BookingStatuses>
        {
            BookingStatuses.Pending, BookingStatuses.Confirmed, BookingStatuses.InternalProcessing, BookingStatuses.WaitingForResponse
        };
        
        private static readonly HashSet<PaymentMethods> PaymentMethodsForCapture = new HashSet<PaymentMethods>
        {
            PaymentMethods.CreditCard
        };
        
        private static readonly HashSet<PaymentMethods> PaymentMethodsForCharge = new HashSet<PaymentMethods>
        {
            PaymentMethods.BankTransfer
        };

        private static readonly HashSet<BookingPaymentStatuses> PaymentStatusesForNotification = new HashSet<BookingPaymentStatuses>
        {
            BookingPaymentStatuses.Authorized, BookingPaymentStatuses.NotPaid
        };


        private readonly IBookingPaymentService _bookingPaymentService;
        private readonly IBookingManagementService _bookingManagementService;
        private readonly IBookingMailingService _bookingMailingService;
        private readonly EdoContext _context;
    }
}