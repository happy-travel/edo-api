using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;
using HappyTravel.Edo.Api.Services.Management;

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.CounterpartyAccountServiceTests
{
    public class AddMoneyTests
    {
        public AddMoneyTests()
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            _edoContextMock = MockEdoContextFactory.Create();
            _mockedEdoContext = _edoContextMock.Object;

            _counterpartyAccountService = new CounterpartyAccountService(_mockedEdoContext, entityLockerMock.Object, 
                Mock.Of<IManagementAuditService>(), Mock.Of<IAccountBalanceAuditService>(), Mock.Of<ICounterpartyBillingNotificationService>());

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(_mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            _edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);
        }

        [Fact]
        public async Task Add_money_with_currency_mismatch_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.AddMoney(
                1, new PaymentData(1, Currencies.EUR, "kek"), _apiCaller);
            
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Add_money_to_unexistent_account_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.AddMoney(
                0, new PaymentData(1, Currencies.USD, "kek"), _apiCaller);
            
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Add_money_with_negative_amount_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.AddMoney(
                1, new PaymentData(-1, Currencies.USD, "kek"), _apiCaller);
            
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Add_money_to_suitable_account_should_increase_balance()
        {
            SetupInitialData();
            var affectedAccount = _mockedEdoContext.CounterpartyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await _counterpartyAccountService.AddMoney(
                1, new PaymentData(1, Currencies.USD, "kek"), _apiCaller);

            Assert.True(isSuccess);
            Assert.Equal(1001, affectedAccount.Balance);
        }


        private void SetupInitialData()
        {
            _edoContextMock
                .Setup(c => c.Counterparties)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Counterparty>
                {
                    new Counterparty
                    {
                        Id = 1
                    },
                    // Having more than one element for predicates to be tested too
                    new Counterparty
                    {
                        Id = 2
                    },
                }));

            _edoContextMock
                .Setup(c => c.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Agency>
                {
                    new Agency
                    {
                        Id = 1,
                        CounterpartyId = 1,
                        ParentId = null,
                    },
                    new Agency
                    {
                        Id = 2,
                        CounterpartyId = 2,
                        ParentId = null,
                    }
                }));

            _edoContextMock
                .Setup(c => c.AgencyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<AgencyAccount>
                {
                    new AgencyAccount
                    {
                        Id = 1,
                        Balance = 0,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        IsActive = true
                    },
                    new AgencyAccount
                    {
                        Id = 1,
                        Balance = 0,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        IsActive = true
                    }
                }));

            _edoContextMock
                .Setup(c => c.CounterpartyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<CounterpartyAccount>
                {
                    new CounterpartyAccount
                    {
                        Id = 1,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 1,
                        IsActive = true
                    },
                    new CounterpartyAccount
                    {
                        Id = 2,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 2,
                        IsActive = true
                    }
                }));
        }

        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly ApiCaller _apiCaller = new ApiCaller(1, ApiCallerTypes.Admin);
        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}
