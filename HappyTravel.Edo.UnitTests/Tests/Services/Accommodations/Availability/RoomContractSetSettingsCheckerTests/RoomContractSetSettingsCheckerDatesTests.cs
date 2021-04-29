using System;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Bookings;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.RoomContractSetSettingsCheckerTests
{
    public class RoomContractSetSettingsCheckerDatesTests
    {
        [Fact]
        public void Passed_checkin_counted_when_no_deadline()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 19);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(default);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Not_yet_passed_checkin_counted_when_no_deadline()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 21);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(default);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.True(allowed);
        }


        [Fact]
        public void Checkin_counted_when_it_is_less_than_deadline()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 19);
            var deadlineDate = new DateTime(2021, 1, 22);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Deadline_counted_when_it_is_less_than_checkin()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 22);
            var deadlineDate = new DateTime(2021, 1, 19);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Deadline_counted_as_passed_when_equal_to_tomorrow()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var deadlineDate = new DateTime(2021, 1, 20);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Deadline_counted_as_passed_when_less_than_tomorrow()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var deadlineDate = new DateTime(2021, 1, 19);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Deadline_counted_as_not_passed_when_greater_than_tomorrow()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var deadlineDate = new DateTime(2021, 1, 21);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.True(allowed);
        }


        [Fact]
        public void Checkin_counted_as_passed_when_equal_to_tomorrow()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 20);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(default);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Checkin_counted_as_passed_when_less_than_tomorrow()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 19);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(default);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.False(allowed);
        }


        [Fact]
        public void Checkin_counted_as_not_passed_when_greater_than_tomorrow()
        {
            var settings = GetSettings(AprMode.DisplayOnly, PassedDeadlineOffersMode.Hide);
            var checkin = new DateTime(2021, 1, 21);
            var tomorrow = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(default);
            var roomSet = GetRoomContractSet(deadline, default);
            var dateTimeProvider = GetProvider(tomorrow);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkin, settings, dateTimeProvider);

            Assert.True(allowed);
        }


        private Deadline GetDeadline(DateTime? deadlineDate)
            => new Deadline(deadlineDate, default, default, default);


        private RoomContractSet GetRoomContractSet(Deadline deadline, bool isApr)
            => new RoomContractSet(default, default, deadline, default, isApr, default, default, default);


        private AccommodationBookingSettings GetSettings(AprMode aprMode, PassedDeadlineOffersMode deadlineMode)
            => new AccommodationBookingSettings(default, aprMode, deadlineMode, default, default, default, default, default);


        private IDateTimeProvider GetProvider(DateTime tomorrow)
        {
            var dateTimeProvider = new Mock<IDateTimeProvider>();
            dateTimeProvider.Setup(p => p.UtcTomorrow()).Returns(tomorrow);
            return dateTimeProvider.Object;
        }
    }
}
