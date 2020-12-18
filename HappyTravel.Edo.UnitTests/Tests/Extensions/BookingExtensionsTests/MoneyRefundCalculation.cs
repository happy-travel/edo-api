using System;
using System.Collections.Generic;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Xunit;
using Booking = HappyTravel.Edo.Data.Bookings.Booking;

namespace HappyTravel.Edo.UnitTests.Tests.Extensions.BookingExtensionsTests
{
    public class MoneyRefundCalculation : IDisposable
    {
        [Fact]
        public void Everything_unknown_should_refund_full_price()
        {
            _booking.Rooms = new List<BookedRoom>
            {
                MakeBookedRoom(
                    deadline: new Deadline(null, null, null, true),
                    price: new MoneyAmount(100m, Currencies.USD))
            };
            var forDate = new DateTime(2020, 1, 1);

            var refundableAmount = _booking.GetRefundableAmount(forDate);

            Assert.Equal(100m, refundableAmount);
        }


        [Fact]
        public void No_policies_before_deadline_should_refund_full_price()
        {
            _booking.Rooms = new List<BookedRoom>
            {
                MakeBookedRoom(
                    deadline: new Deadline(new DateTime(2020, 1, 2), null, null, true),
                    price: new MoneyAmount(100m, Currencies.USD))
            };
            var forDate = new DateTime(2020, 1, 1);

            var refundableAmount = _booking.GetRefundableAmount(forDate);

            Assert.Equal(100m, refundableAmount);
        }

        
        [Fact]
        public void No_policies_after_deadline_should_refund_nothing()
        {
            _booking.Rooms = new List<BookedRoom>
            {
                MakeBookedRoom(
                    deadline: new Deadline(new DateTime(2020, 1, 1), null, null, true),
                    price: new MoneyAmount(100m, Currencies.USD))
            };
            var forDate = new DateTime(2020, 1, 2);

            var refundableAmount = _booking.GetRefundableAmount(forDate);

            Assert.Equal(0m, refundableAmount);
        }

        
        [Fact]
        public void Existing_policies_should_ignore_deadline_date()
        {
            _booking.Rooms = new List<BookedRoom>
            {
                MakeBookedRoom(
                    deadline: new Deadline(
                        new DateTime(2020, 1, 1),
                        new List<CancellationPolicy>
                        {
                            new CancellationPolicy(fromDate: new DateTime(2020, 1, 3), 0.3d)
                        }, null, true),
                    price: new MoneyAmount(100m, Currencies.USD))
            };
            var forDate = new DateTime(2020, 1, 2);

            var refundableAmount = _booking.GetRefundableAmount(forDate);

            Assert.Equal(100m, refundableAmount);
        }
        

        [Theory]
        [InlineData(2, 100)]
        [InlineData(3, 70)]
        [InlineData(4, 70)]
        [InlineData(5, 40)]
        [InlineData(6, 40)]
        [InlineData(7, 40)]
        [InlineData(8, 20)]
        [InlineData(9, 20)]
        [InlineData(10, 0)]
        [InlineData(11, 0)]
        public void Existing_policies_should_show_refund_according_date(int day, int expectedRefundableAmount)
        {
            _booking.Rooms = new List<BookedRoom>
            {
                MakeBookedRoom(
                    deadline: new Deadline(
                        new DateTime(2020, 1, 3),
                        new List<CancellationPolicy>
                        {
                            new CancellationPolicy(fromDate: new DateTime(2020, 1, 3), 30d),
                            new CancellationPolicy(fromDate: new DateTime(2020, 1, 5), 60d),
                            new CancellationPolicy(fromDate: new DateTime(2020, 1, 8), 80d),
                            new CancellationPolicy(fromDate: new DateTime(2020, 1, 10), 100d),
                        }, null, true),
                    price: new MoneyAmount(100m, Currencies.USD))
            };
            var forDate = new DateTime(2020, 1, day);

            var refundableAmount = _booking.GetRefundableAmount(forDate);

            Assert.Equal(expectedRefundableAmount, refundableAmount);
        }


        private BookedRoom MakeBookedRoom(Deadline deadline, MoneyAmount price, List<CancellationPolicy> policies = null) =>
            new BookedRoom(default, default, price, default, default, default, default, new List<KeyValuePair<string, string>>(), deadline, new List<Passenger>(), default);


        private readonly Booking _booking = new Booking{Currency = Currencies.USD};

        public void Dispose()
        {
            
        }
    }
}
