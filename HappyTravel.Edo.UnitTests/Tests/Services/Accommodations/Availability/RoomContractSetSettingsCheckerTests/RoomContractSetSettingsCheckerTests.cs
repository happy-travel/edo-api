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
    public class RoomContractSetSettingsCheckerTests
    {
        // Display tests
        [Theory]
        // Deadline date does not matter
        [InlineData(19, 20, PassedDeadlineOffersMode.DisplayOnly)]
        [InlineData(19, 20, PassedDeadlineOffersMode.CardAndAccountPurchases)]
        [InlineData(19, 20, PassedDeadlineOffersMode.CardPurchasesOnly)]
        // Deadline did not come yet
        [InlineData(21, 20, PassedDeadlineOffersMode.Hide)]
        public void Display_should_be_allowed_when_deadline_conditions_met(int deadlineDay, int tommorowDay, PassedDeadlineOffersMode deadlineMode)
        {
            var settings = GetSettings(AprMode.DisplayOnly, deadlineMode);
            var deadlineDate = new DateTime(2021, 1, deadlineDay);
            var tommorowDate = new DateTime(2021, 1, tommorowDay);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, false);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.True(allowed);
        }


        [Theory]
        // Show apr room sets
        [InlineData(AprMode.DisplayOnly, true)]
        [InlineData(AprMode.CardAndAccountPurchases, true)]
        [InlineData(AprMode.CardPurchasesOnly, true)]
        // Room set is not apr
        [InlineData(AprMode.Hide, false)]
        public void Display_should_be_allowed_when_apr_conditions_met(AprMode aprMode, bool isRoomSetApr)
        {
            var settings = GetSettings(aprMode, PassedDeadlineOffersMode.DisplayOnly);
            var deadlineDate = new DateTime(2021, 1, 21);
            var tommorowDate = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, isRoomSetApr);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.True(allowed);
        }


        [Theory]
        [InlineData(19, 20, PassedDeadlineOffersMode.Hide, AprMode.DisplayOnly, true)] // Apr ok, deadline not ok
        [InlineData(19, 20, PassedDeadlineOffersMode.Hide, AprMode.Hide, true)] // Apr not ok, deadline not ok
        [InlineData(21, 20, PassedDeadlineOffersMode.Hide, AprMode.Hide, true)] // Apr not ok, deadline ok
        public void Display_should_not_be_allowed_when_any_condition_not_met(int deadlineDay, int tommorowDay, PassedDeadlineOffersMode deadlineMode,
            AprMode aprMode, bool isRoomSetApr)
        {
            var settings = GetSettings(aprMode, deadlineMode);
            var deadlineDate = new DateTime(2021, 1, deadlineDay);
            var tommorowDate = new DateTime(2021, 1, tommorowDay);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, isRoomSetApr);

            var allowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.False(allowed);
        }


        // Evaluation tests
        [Theory]
        // Deadline date does not matter
        [InlineData(19, 20, PassedDeadlineOffersMode.CardAndAccountPurchases)]
        [InlineData(19, 20, PassedDeadlineOffersMode.CardPurchasesOnly)]
        // Deadline did not come yet
        [InlineData(21, 20, PassedDeadlineOffersMode.Hide)]
        public void Eval_should_be_allowed_when_deadline_conditions_met(int deadlineDay, int tommorowDay, PassedDeadlineOffersMode deadlineMode)
        {
            var settings = GetSettings(AprMode.DisplayOnly, deadlineMode);
            var deadlineDate = new DateTime(2021, 1, deadlineDay);
            var tommorowDate = new DateTime(2021, 1, tommorowDay);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, false);

            var allowed = RoomContractSetSettingsChecker.IsEvaluationAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.True(allowed);
        }


        [Theory]
        // Show apr room sets
        [InlineData(AprMode.CardAndAccountPurchases, true)]
        [InlineData(AprMode.CardPurchasesOnly, true)]
        // Room set is not apr
        [InlineData(AprMode.Hide, false)]
        public void Eval_should_be_allowed_when_apr_conditions_met(AprMode aprMode, bool isRoomSetApr)
        {
            var settings = GetSettings(aprMode, PassedDeadlineOffersMode.DisplayOnly);
            var deadlineDate = new DateTime(2021, 1, 21);
            var tommorowDate = new DateTime(2021, 1, 20);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, isRoomSetApr);

            var allowed = RoomContractSetSettingsChecker.IsEvaluationAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.True(allowed);
        }


        [Theory]
        [InlineData(19, 20, PassedDeadlineOffersMode.Hide, AprMode.CardAndAccountPurchases, true)] // Apr ok, deadline not ok
        [InlineData(19, 20, PassedDeadlineOffersMode.Hide, AprMode.Hide, true)] // Apr not ok, deadline not ok
        [InlineData(21, 20, PassedDeadlineOffersMode.Hide, AprMode.Hide, true)] // Apr not ok, deadline ok
        public void Eval_should_not_be_allowed_when_any_condition_not_met(int deadlineDay, int tommorowDay, PassedDeadlineOffersMode deadlineMode,
            AprMode aprMode, bool isRoomSetApr)
        {
            var settings = GetSettings(aprMode, deadlineMode);
            var deadlineDate = new DateTime(2021, 1, deadlineDay);
            var tommorowDate = new DateTime(2021, 1, tommorowDay);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, isRoomSetApr);

            var allowed = RoomContractSetSettingsChecker.IsEvaluationAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.False(allowed);
        }


        // Display only tests
        [Theory]
        [InlineData(21, 20, PassedDeadlineOffersMode.CardAndAccountPurchases, AprMode.DisplayOnly, true)] // Apr display only
        [InlineData(19, 20, PassedDeadlineOffersMode.DisplayOnly, AprMode.CardAndAccountPurchases, true)] // Deadline display only
        [InlineData(19, 20, PassedDeadlineOffersMode.DisplayOnly, AprMode.DisplayOnly, true)] // Both apr and deadline display only
        public void Display_should_be_allowed_but_eval_not_when_display_only_mode(int deadlineDay, int tommorowDay, PassedDeadlineOffersMode deadlineMode,
            AprMode aprMode, bool isRoomSetApr)
        {
            var settings = GetSettings(aprMode, deadlineMode);
            var deadlineDate = new DateTime(2021, 1, deadlineDay);
            var tommorowDate = new DateTime(2021, 1, tommorowDay);
            var deadline = GetDeadline(deadlineDate);
            var dateTimeProvider = GetProvider(tommorowDate);
            var roomSet = GetRoomContractSet(deadline, isRoomSetApr);

            var displayAllowed = RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);
            var evaluationAllowed = RoomContractSetSettingsChecker.IsEvaluationAllowed(roomSet, deadlineDate.AddDays(1), settings, dateTimeProvider);

            Assert.True(displayAllowed);
            Assert.False(evaluationAllowed);
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
