using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct ListOfBookingIds
    {
        public ListOfBookingIds(List<int> bookingIds)
        {
            BookingIds = bookingIds ?? new List<int>(0);
        }

        /// <summary>
        ///     List of booking ids
        /// </summary>
        public List<int> BookingIds { get; }
    }
}