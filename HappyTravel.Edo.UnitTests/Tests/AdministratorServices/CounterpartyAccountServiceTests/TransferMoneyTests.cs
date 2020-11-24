using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
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

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.CounterpartyAccountServiceTests
{
    public class TransferMoneyTests
    {
        public TransferMoneyTests()
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            var edoContextMock = MockEdoContextFactory.Create();
            _edoContextMock = edoContextMock;
            _mockedEdoContext = edoContextMock.Object;

            _counterpartyAccountService = new CounterpartyAccountService(_mockedEdoContext, entityLockerMock.Object, Mock.Of<IAccountBalanceAuditService>());

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(_mockedEdoContext);
            dbFacade.Setup(d => d.CreateExecutionStrategy()).Returns(strategy);
            edoContextMock.Setup(c => c.Database).Returns(dbFacade.Object);
        }

        [Fact]
        public async Task Transfer_to_default_agency_currency_mismatch_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.TransferToDefaultAgency(
                3, new MoneyAmount(1, Currencies.EUR), _user);

            Assert.True(isFailure);
        }

        [Fact]
        public async Task Transfer_to_default_agency_which_not_exists_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.TransferToDefaultAgency(
                1, new MoneyAmount(1, Currencies.USD), _user);

            Assert.True(isFailure);
        }

        [Fact]
        public async Task Transfer_to_default_agency_without_account_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.TransferToDefaultAgency(
                2, new MoneyAmount(1, Currencies.USD), _user);

            Assert.True(isFailure);
        }

        [Fact]
        public async Task Transfer_to_default_agency_with_different_currency_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.TransferToDefaultAgency(
                4, new MoneyAmount(1, Currencies.USD), _user);

            Assert.True(isFailure);
        }

        [Fact]
        public async Task Transfer_to_default_agency_with_negative_amount_should_fail()
        {
            SetupInitialData();

            var (_, isFailure, error) = await _counterpartyAccountService.TransferToDefaultAgency(
                3, new MoneyAmount(-1, Currencies.USD), _user);

            Assert.True(isFailure);
        }

        [Fact]
        public async Task Transfer_to_default_agency_should_affect_the_accounts()
        {
            SetupInitialData();
            var counterpartyAccount = _mockedEdoContext.CounterpartyAccounts.Single(a => a.Id == 3);
            var agencyAccount = _mockedEdoContext.AgencyAccounts.Single(a => a.Id == 3);

            var (isSuccess, _, error) = await _counterpartyAccountService.TransferToDefaultAgency(
                3, new MoneyAmount(1, Currencies.USD), _user);

            Assert.True(isSuccess);
            Assert.Equal(999, counterpartyAccount.Balance);
            Assert.Equal(1, agencyAccount.Balance);
        }


        private void SetupInitialData()
        {
            _edoContextMock
                .Setup(c => c.Counterparties)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Counterparty>
                {
                    new Counterparty
                    {
                        Id = 1,
                        Name = "WothoutAgency"
                    },
                    new Counterparty
                    {
                        Id = 2,
                        Name = "WithBadAgency"
                    },
                    new Counterparty
                    {
                        Id = 3,
                        Name = "WithGoodAgency"
                    },
                    new Counterparty
                    {
                        Id = 4,
                        Name = "WithDifferentCurrencies"
                    }
                }));

            _edoContextMock
                .Setup(c => c.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Agency>
                {
                    new Agency
                    {
                        Id = 2,
                        CounterpartyId = 2,
                        Name = "BadAgencyNoAccount",
                        ParentId = null,
                    },
                    new Agency
                    {
                        Id = 3,
                        CounterpartyId = 3,
                        Name = "GoodAgencyWithAccount",
                        ParentId = null,
                    },
                    new Agency
                    {
                        Id = 4,
                        CounterpartyId = 4,
                        Name = "WithDifferentCurrancy",
                        ParentId = null,
                    }
                }));

            _edoContextMock
                .Setup(c => c.AgencyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<AgencyAccount>
                {
                    new AgencyAccount
                    {
                        Id = 3,
                        Balance = 0,
                        Currency = Currencies.USD,
                        AgencyId = 3,
                        IsActive = true
                    },
                    new AgencyAccount
                    {
                        Id = 4,
                        Balance = 0,
                        Currency = Currencies.EUR,
                        AgencyId = 4,
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
                    },
                    new CounterpartyAccount
                    {
                        Id = 3,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 3,
                        IsActive = true
                    },
                    new CounterpartyAccount
                    {
                        Id = 4,
                        Balance = 1000,
                        Currency = Currencies.USD,
                        CounterpartyId = 4,
                        IsActive = true
                    }
                }));
        }

        private readonly Mock<EdoContext> _edoContextMock;
        private readonly EdoContext _mockedEdoContext;
        private readonly UserInfo _user = new UserInfo(1, UserTypes.Admin);
        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}
