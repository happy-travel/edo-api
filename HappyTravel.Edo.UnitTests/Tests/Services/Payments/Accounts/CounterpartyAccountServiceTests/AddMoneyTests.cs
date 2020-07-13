using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Stubs;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Payments.Accounts.CounterpartyAccountServiceTests
{
    public class AddMoneyTests
    {
        public AddMoneyTests(Mock<EdoContext> edoContextMock)
        {
            var entityLocker = new EntityLockerStub();

            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            _counterpartyAccountService = new CounterpartyAccountService(_mockedEdoContext, entityLocker, Mock.Of<IAccountBalanceAuditService>());

            var strategy = new ExecutionStrategyStub();

            var dbFacade = new Mock<DatabaseFacade>(_mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);

        }

        [Fact]
        public async Task Add_money_with_currency_mismatch_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.AddMoney(
                1, new PaymentData(1, Currencies.EUR, "kek"), _user);
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Add_money_to_unexistent_account_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.AddMoney(
                0, new PaymentData(1, Currencies.USD, "kek"), _user);
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Add_money_with_negative_amount_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.AddMoney(
                1, new PaymentData(-1, Currencies.USD, "kek"), _user);
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Add_money_to_suitable_account_should_increase_balance()
        {
            SetupInitialData();
            var affectedAccount = _mockedEdoContext.CounterpartyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await _counterpartyAccountService.AddMoney(
                1, new PaymentData(1, Currencies.USD, "kek"), _user);

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
                .Setup(c => c.PaymentAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<PaymentAccount>
                {
                    new PaymentAccount
                    {
                        Id = 1,
                        Balance = 0,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        CreditLimit = 0
                    },
                    new PaymentAccount
                    {
                        Id = 1,
                        Balance = 0,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        CreditLimit = 0
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
                        CounterpartyId = 1
                    },
                    new CounterpartyAccount
                    {
                        Id = 2,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 2
                    }
                }));
        }

        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly UserInfo _user = new UserInfo(1, UserTypes.Admin);
        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}
