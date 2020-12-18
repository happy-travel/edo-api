using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Payments.Accounts.AccountPaymentServiceTests
{
    public class ChargeMoneyTests : IDisposable
    {
        public ChargeMoneyTests()
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));
            
            var edoContextMock = MockEdoContextFactory.Create();
            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            var accountPaymentProcessingService = new AccountPaymentProcessingService(
                _mockedEdoContext, entityLockerMock.Object, Mock.Of<IAccountBalanceAuditService>());

            var accountManagementService = new AccountManagementService(_mockedEdoContext, Mock.Of<IDateTimeProvider>(),
                Mock.Of<ILogger<AccountManagementService>>(), Mock.Of<IAdministratorContext>(), Mock.Of<IManagementAuditService>(),
                entityLockerMock.Object);

            var bookingRecordsManager = new BookingRecordsManager(edoContextMock.Object, Mock.Of<IDateTimeProvider>(), Mock.Of<ITagProcessor>(),
                Mock.Of<IAccommodationService>(), Mock.Of<IAccommodationBookingSettingsService>(),
                Mock.Of<IAppliedBookingMarkupRecordsManager>(), Mock.Of<ISupplierOrderService>());

            _accountPaymentService = new AccountPaymentService(accountPaymentProcessingService, _mockedEdoContext,
                Mock.Of<IDateTimeProvider>(), accountManagementService, entityLockerMock.Object, bookingRecordsManager);

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
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Payment>()));
        }


        [Fact]
        public async Task Successful_charge_should_lower_balance()
        {
            var (isSuccess, _, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isSuccess);
            Assert.Equal(900m, _account.Balance);
        }


        [Fact]
        public async Task Successful_charge_should_set_booking_status()
        {
            var (isSuccess, _, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isSuccess);
            Assert.Equal(BookingPaymentStatuses.Captured, _booking.PaymentStatus);
        }


        [Fact]
        public async Task Successful_charge_should_set_payment_status()
        {
            var (isSuccess, _, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isSuccess);
            Assert.True(_mockedEdoContext.Payments.Any());
            Assert.Equal(PaymentStatuses.Captured, _mockedEdoContext.Payments.First().Status);
        }


        [Fact]
        public async Task Charge_from_account_with_balance_less_than_cost_should_succeed()
        {
            _account.Balance = 25m;

            var (isSuccess, _, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isSuccess);
            Assert.Equal(-75m, _account.Balance);
        }


        [Fact]
        public async Task Charge_from_account_with_balance_insufficent_should_fail()
        {
            var startingBalance = _account.Balance = 24m;

            var (_, isFailure, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isFailure);
            Assert.Equal(startingBalance, _account.Balance);
        }


        [Fact]
        public async Task Charge_from_nonexistent_booking_should_fail()
        {
            var (_, isFailure, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode + " wrong code", _agent, Ip);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Charge_from_paid_booking_should_fail()
        {
            _booking.PaymentStatus = BookingPaymentStatuses.Captured;

            var (_, isFailure, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Charge_from_cancelled_booking_should_fail()
        {
            _booking.Status = BookingStatuses.Cancelled;

            var (_, isFailure, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Charge_from_credic_card_booking_should_fail()
        {
            _booking.PaymentMethod = PaymentMethods.CreditCard;

            var (_, isFailure, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Charge_when_no_account_with_booking_currency_should_fail()
        {
            _booking.Currency = Currencies.EUR;

            var (_, isFailure, _, error) = await _accountPaymentService.Charge(_booking.ReferenceCode, _agent, Ip);

            Assert.True(isFailure);
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
            Status = BookingStatuses.Confirmed
        };

        private readonly AgencyAccount _account = new AgencyAccount
        {
            Id = 1,
            Balance = 1000,
            Currency = Currencies.USD,
            AgencyId = 1,
            IsActive = true
        };

        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly AccountPaymentService _accountPaymentService;
        private readonly AgentContext _agent = new AgentContext(1, "", "", "", "", "", 1, "", 1, true, InAgencyPermissions.All);
        private const string Ip = "1";
    }
}
