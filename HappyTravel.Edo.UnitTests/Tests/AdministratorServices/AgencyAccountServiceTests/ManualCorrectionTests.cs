using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.AgencyAccountServiceTests
{
    public class ManualCorrectionTests
    {
        [Fact]
        public async Task Increase_money_with_currency_mismatch_should_fail()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);

            var (_, isFailure, error) = await agencyAccountService.IncreaseManually(
                1, new PaymentData(1, Currencies.EUR, "not empty reason"), _apiCaller);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Increase_money_to_not_existing_account_should_fail()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);

            var (_, isFailure, error) = await agencyAccountService.IncreaseManually(
                0, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Increase_money_with_negative_amount_should_fail()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);

            var (_, isFailure, error) = await agencyAccountService.IncreaseManually(
                1, new PaymentData(-1, Currencies.USD, "not empty reason"), _apiCaller);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Increase_money_to_suitable_account_should_increase_balance()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);
            var affectedAccount = context.AgencyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await agencyAccountService.IncreaseManually(
                1, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);

            Assert.True(isSuccess);
            Assert.Equal(121, affectedAccount.Balance);
        }


        [Fact]
        public async Task Decrease_money_with_currency_mismatch_should_fail()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);

            var (_, isFailure, error) = await agencyAccountService.DecreaseManually(
                1, new PaymentData(1, Currencies.EUR, "not empty reason"), _apiCaller);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Decrease_money_to_not_existing_account_should_fail()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);

            var (_, isFailure, error) = await agencyAccountService.DecreaseManually(
                0, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Decrease_money_with_negative_amount_should_fail()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);

            var (_, isFailure, error) = await agencyAccountService.DecreaseManually(
                1, new PaymentData(-1, Currencies.USD, "not empty reason"), _apiCaller);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Decrease_money_from_suitable_account_should_decrease_balance()
        {
            var context = GetDbContextMock();
            var agencyAccountService = GetAgencyAccountServiceMock(context);
            var affectedAccount = context.AgencyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await agencyAccountService.DecreaseManually(
                1, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);

            Assert.True(isSuccess);
            Assert.Equal(119, affectedAccount.Balance);
        }


        private AgencyAccountService GetAgencyAccountServiceMock(EdoContext dbContext)
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            return new AgencyAccountService(dbContext, entityLockerMock.Object, Mock.Of<IManagementAuditService>(), Mock.Of<IAccountBalanceAuditService>());
        }


        private EdoContext GetDbContextMock()
        {
            var edoContextMock = new Mock<EdoContext>(new DbContextOptions<EdoContext>());

            var strategy = new ExecutionStrategyMock();

            var dbFacade = new Mock<DatabaseFacade>(edoContextMock.Object);
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

            edoContextMock
                .Setup(c => c.AgencyAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<AgencyAccount>
                {
                    new AgencyAccount
                    {
                        Id = 1,
                        Balance = 120,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        IsActive = true
                    },
                    new AgencyAccount
                    {
                        Id = 2,
                        Balance = 120,
                        Currency = Currencies.USD,
                        AgencyId = 2,
                        IsActive = true
                    }
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
                        Currency = Currencies.USD,
                        CounterpartyId = 2,
                        IsActive = true
                    }
                }));

            return edoContextMock.Object;
        }


        private readonly ApiCaller _apiCaller = new ApiCaller(1, ApiCallerTypes.Admin);
    }
}