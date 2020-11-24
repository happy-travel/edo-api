using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Booking = HappyTravel.Edo.Data.Booking.Booking;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Payments.Accounts.AccountPaymentServiceTests
{
    public class RefundMoneyTests : IDisposable
    {
        public RefundMoneyTests()
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            var edoContextMock = MockEdoContextFactory.Create();
            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            var accountPaymentProcessingService = new AccountPaymentProcessingService(
                _mockedEdoContext, entityLockerMock.Object, Mock.Of<IAccountBalanceAuditService>());

            _dateTimeProviderMock = new Mock<IDateTimeProvider>();
            _dateTimeProviderMock.Setup(d => d.UtcNow()).Returns(new DateTime(2020, 1, 1));

            var accountManagementService = new AccountManagementService(_mockedEdoContext, _dateTimeProviderMock.Object,
                Mock.Of<ILogger<AccountManagementService>>(), Mock.Of<IAdministratorContext>(), Mock.Of<IManagementAuditService>(),
                entityLockerMock.Object);

            var bookingRecordsManager = new BookingRecordsManager(edoContextMock.Object, _dateTimeProviderMock.Object, Mock.Of<ITagProcessor>(),
                Mock.Of<IAccommodationService>(), Mock.Of<IAccommodationBookingSettingsService>());

            _accountPaymentService = new AccountPaymentService(accountPaymentProcessingService, _mockedEdoContext,
                _dateTimeProviderMock.Object, accountManagementService, entityLockerMock.Object, bookingRecordsManager);

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(_mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);

            _edoContextMock
                .Setup(c => c.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Agency>
                {
                    new Agency
                    {
                        Id = 1,
                        Name = "Agency",
                        ParentId = null,
                    },
                }));

            _edoContextMock
                .Setup(c => c.AgencyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<AgencyAccount>
                {
                    _account,
                }));

            _edoContextMock
                .Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Booking>
                {
                    _booking,
                }));

            _edoContextMock
                .Setup(c => c.Payments)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Payment>
                {
                    _payment,
                }));
        }


        [Fact]
        public async Task Successful_refund_should_rise_balance()
        {
            var (isSuccess, _, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isSuccess);
            Assert.Equal(1100m, _account.Balance);
        }


        [Fact]
        public async Task Successful_refund_should_change_payment_status()
        {
            var (isSuccess, _, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isSuccess);
            Assert.Equal(PaymentStatuses.Refunded, _payment.Status);
        }


        [Fact]
        public async Task Refund_booking_with_not_capture_status_should_not_affect_balance()
        {
            _booking.PaymentStatus = BookingPaymentStatuses.NotPaid;

            var (isSuccess, _, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isSuccess);
            Assert.Equal(1000m, _account.Balance);
        }


        [Fact]
        public async Task Refund_booking_with_not_bank_transfer_should_fail()
        {
            _booking.PaymentMethod = PaymentMethods.CreditCard;

            var (_, isFailure, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isFailure);
            Assert.Equal(1000m, _account.Balance);
        }


        [Fact]
        public async Task Refund_booking_without_account_with_currency_should_fail()
        {
            _booking.Currency = Currencies.EUR;

            var (_, isFailure, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isFailure);
            Assert.Equal(1000m, _account.Balance);
        }


        [Fact]
        public async Task Refund_booking_without_payment_should_fail()
        {
            _payment.BookingId = 0;

            var (_, isFailure, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isFailure);
            Assert.Equal(1000m, _account.Balance);
        }


        [Fact]
        public async Task Refund_booking_should_respect_cancellation_policy()
        {
            _dateTimeProviderMock.Setup(p => p.UtcNow()).Returns(new DateTime(2020, 1, 2));

            var (isSuccess, _, error) = await _accountPaymentService.Refund(_booking, _agent.ToUserInfo());

            Assert.True(isSuccess);
            Assert.Equal(1060m, _account.Balance);
        }


        public void Dispose()
        {

        }


        private readonly Booking _booking = new Booking
        {
            Id = 1,
            Currency = Currencies.USD,
            AgencyId = 1,
            CounterpartyId = 1,
            AgentId = 1,
            ReferenceCode = "okay booking",
            TotalPrice = 100,
            PaymentMethod = PaymentMethods.BankTransfer,
            Status = BookingStatuses.Confirmed,
            PaymentStatus = BookingPaymentStatuses.Captured,
            Rooms = new List<BookedRoom>
            {
                new BookedRoom(default, default, new MoneyAmount(100m, Currencies.USD), default, default, default, default,
                    new List<KeyValuePair<string, string>>(),
                    new Data.Booking.Deadline(
                        new DateTime(2020, 1, 2), 
                        new List<Data.Booking.CancellationPolicy>{new Data.Booking.CancellationPolicy(new DateTime(2020, 1, 2), 40d)}),
                    new EditableList<Passenger>(), default)
            }
        };

        private readonly AgencyAccount _account = new AgencyAccount
        {
            Id = 1,
            Balance = 1000,
            Currency = Currencies.USD,
            AgencyId = 1,
            IsActive = true
        };

        private readonly Payment _payment = new Payment
        {
            Id = 1,
            BookingId = 1,
            Amount = 100,
            Status = PaymentStatuses.Captured,
        };

        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly AccountPaymentService _accountPaymentService;
        private readonly AgentContext _agent = new AgentContext(1, "", "", "", "", "", 1, "", 1, true, InAgencyPermissions.All);
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    }
}
