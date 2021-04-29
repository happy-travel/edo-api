using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.CounterpartyAccountServiceTests
{
    public class GetBalanceTests
    {
        public GetBalanceTests()
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            var edoContextMock = MockEdoContextFactory.Create();
            var mockedEdoContext = edoContextMock.Object;

            _counterpartyAccountService = new CounterpartyAccountService(mockedEdoContext, entityLockerMock.Object, 
                Mock.Of<IManagementAuditService>(), Mock.Of<IAccountBalanceAuditService>(), Mock.Of<ICounterpartyBillingNotificationService>());

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);

            edoContextMock
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

            edoContextMock
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
                        Currency = Currencies.EUR,
                        CounterpartyId = 2,
                        IsActive = true
                    },
                }));
        }

        [Fact]
        public async Task Existing_currency_balance_should_be_shown()
        {
            var (isSuccess, _, balanceInfo) = await _counterpartyAccountService.GetBalance(1, Currencies.USD);
            
            Assert.True(isSuccess);
            Assert.Equal(1000, balanceInfo.Balance);
        }

        [Fact]
        public async Task Not_existing_currency_balance_show_should_fail()
        {
            var (_, isFailure) = await _counterpartyAccountService.GetBalance(1, Currencies.EUR);
            
            Assert.True(isFailure);
        }


        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}
