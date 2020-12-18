using System;
using System.Linq;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Money.Helpers;
using HappyTravel.Money.Models;
using Booking = HappyTravel.Edo.Data.Bookings.Booking;

namespace HappyTravel.Edo.Api.Extensions
{
    public static class BookingExtensions
    {
        public static decimal GetRefundableAmount(this Booking booking, DateTime forDate)
        {
            var refundableAmount = booking.Rooms.Sum(room => room.Price.Amount * 
                (decimal)room.DeadlineDetails.GetRefundableFraction(forDate));
            
            return MoneyRounder.Ceil(refundableAmount, booking.Currency);
        }


        public static MoneyAmount GetCancellationPenalty(this Booking booking, DateTime forDate)
        {
            var penalty = booking.TotalPrice - booking.GetRefundableAmount(forDate);
            return new MoneyAmount(penalty, booking.Currency);
        }
        
        
        private static double GetRefundableFraction(this Deadline deadline, DateTime forDate) =>
            1d - deadline.GetNonRefundableFraction(forDate);


        private static double GetNonRefundableFraction(this Deadline deadline, DateTime forDate)
        {
            if (!deadline.Policies.Any())
                return deadline.Date <= forDate
                    ? Whole
                    : Nothing;

            var appliedPolicy = deadline.Policies.OrderBy(p => p.FromDate).LastOrDefault(p => p.FromDate <= forDate);

            return appliedPolicy == default
                ? Nothing
                : appliedPolicy.Percentage / 100;
        }

        private const double Whole = 1d;
        private const double Nothing = 0d;
    }
}
