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
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Payments.Accounts.AccountPaymentServiceTests
{
    public class TransferMoneyTests : IDisposable
    {
        public TransferMoneyTests()
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            var edoContextMock = MockEdoContextFactory.Create();
            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            var accountPaymentProcessingService = new AccountPaymentProcessingService(
                _mockedEdoContext, entityLockerMock.Object, Mock.Of<IAccountBalanceAuditService>());

            var bookingRecordsManager = new BookingRecordsManager(edoContextMock.Object, Mock.Of<IDateTimeProvider>(), Mock.Of<ITagProcessor>(),
                Mock.Of<IAccommodationService>(), Mock.Of<IAccommodationBookingSettingsService>());

            _accountPaymentService = new AccountPaymentService(accountPaymentProcessingService, _mockedEdoContext,
                Mock.Of<IDateTimeProvider>(), Mock.Of<IAccountManagementService>(), entityLockerMock.Object, bookingRecordsManager);

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
                        Name = "RootAgency",
                        ParentId = null,
                    },
                    new Agency
                    {
                        Id = 2,
                        Name = "ChildAgency",
                        ParentId = 1,
                    },
                    new Agency
                    {
                        Id = 3,
                        Name = "UnrelatedAgency",
                        ParentId = null,
                    },
                }));

            _edoContextMock
                .Setup(c => c.AgencyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<AgencyAccount>
                {
                    new AgencyAccount
                    {
                        Id = 1,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        AgencyId = 1,
                        IsActive = true
                    },
                    new AgencyAccount
                    {
                        Id = 2,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        IsActive = true
                    },
                    new AgencyAccount
                    {
                        Id = 3,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        AgencyId = 3,
                        IsActive = true
                    },
                    new AgencyAccount
                    {
                        Id = 4,
                        Balance = 1000,
                        Currency = Currencies.EUR,
                        AgencyId = 2,
                        IsActive = true
                    },
                }));
        }


        [Fact]
        public async Task Nonexistent_recipient_account_should_fail()
        {
            var agent = GetAgentForAgency(1);
            
            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(0, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Agent_transfer_from_different_agency_should_fail()
        {
            var agent = GetAgentForAgency(2);

            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Negative_amount_transfer_should_fail()
        {
            var agent = GetAgentForAgency(1);
            
            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(-1m, Currencies.USD), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Transfer_to_not_child_agency_should_fail()
        {
            var agent = GetAgentForAgency(1);
            
            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(3, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Transfer_to_account_with_different_currency_should_fail()
        {
            var agent = GetAgentForAgency(1);
            
            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(4, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Transfer_amount_with_different_currency_should_fail()
        {
            var agent = GetAgentForAgency(1);
            
            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(1m, Currencies.EUR), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Transfer_when_balance_insufficient_should_fail()
        {
            var agent = GetAgentForAgency(1);
            
            var (_, isFailure, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(1000000m, Currencies.USD), agent);

            Assert.True(isFailure);
        }


        [Fact]
        public async Task Correct_transfer_should_succeed()
        {
            var agent = GetAgentForAgency(1);
            
            var (isSuccess, _, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isSuccess);
        }


        [Fact]
        public async Task Correct_transfer_should_subtract_correct_value()
        {
            var payerAccount = _mockedEdoContext.AgencyAccounts.Single(a => a.Id == 1);
            var agent = GetAgentForAgency(1);

            var (isSuccess, _, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isSuccess);
            Assert.Equal(999m, payerAccount.Balance);
        }


        [Fact]
        public async Task Correct_transfer_should_add_correct_value()
        {
            var recipientAccount = _mockedEdoContext.AgencyAccounts.Single(a => a.Id == 2);
            var agent = GetAgentForAgency(1);

            var (isSuccess, _, error) = await _accountPaymentService.TransferToChildAgency(2, new MoneyAmount(1m, Currencies.USD), agent);

            Assert.True(isSuccess);
            Assert.Equal(1001m, recipientAccount.Balance);
        }


        private AgentContext GetAgentForAgency(int agencyId)
        {
            return new AgentContext(1, "", "", "", "", "", 1, "", agencyId, true, InAgencyPermissions.All);
        }

        public void Dispose()
        {

        }

        private Mock<EdoContext> _edoContextMock;
        private EdoContext _mockedEdoContext;
        private IAccountPaymentService _accountPaymentService;
    }
}
