using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
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

namespace HappyTravel.Edo.UnitTests.Tests.AdministratorServices.CounterpartyAccountServiceTests
{
    public class ManualCorrectionTests
    {
        [Fact]
        public async Task Increase_money_with_currency_mismatch_should_fail()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);

            var (_, isFailure, error) = await counterpartyAccountService.IncreaseManually(
                1, new PaymentData(1, Currencies.EUR, "not empty reason"), _apiCaller);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Increase_money_to_not_existing_account_should_fail()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);

            var (_, isFailure, error) = await counterpartyAccountService.IncreaseManually(
                0, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Increase_money_with_negative_amount_should_fail()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);

            var (_, isFailure, error) = await counterpartyAccountService.IncreaseManually(
                1, new PaymentData(-1, Currencies.USD, "not empty reason"), _apiCaller);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Increase_money_to_suitable_account_should_increase_balance()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);
            var affectedAccount = context.CounterpartyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await counterpartyAccountService.IncreaseManually(
                1, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);

            Assert.True(isSuccess);
            Assert.Equal(1001, affectedAccount.Balance);
        }


        [Fact]
        public async Task Decrease_money_with_currency_mismatch_should_fail()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);

            var (_, isFailure, error) = await counterpartyAccountService.DecreaseManually(
                1, new PaymentData(1, Currencies.EUR, "not empty reason"), _apiCaller);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Decrease_money_to_not_existing_account_should_fail()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);

            var (_, isFailure, error) = await counterpartyAccountService.DecreaseManually(
                0, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Decrease_money_with_negative_amount_should_fail()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);

            var (_, isFailure, error) = await counterpartyAccountService.DecreaseManually(
                1, new PaymentData(-1, Currencies.USD, "not empty reason"), _apiCaller);
            
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Decrease_money_from_suitable_account_should_decrease_balance()
        {
            var context = GetDbContextMock();
            var counterpartyAccountService = GetAccountService(context);
            var affectedAccount = context.CounterpartyAccounts.Single(a => a.Id == 1);

            var (isSuccess, _, error) = await counterpartyAccountService.DecreaseManually(
                1, new PaymentData(1, Currencies.USD, "not empty reason"), _apiCaller);

            Assert.True(isSuccess);
            Assert.Equal(999, affectedAccount.Balance);
        }


        private CounterpartyAccountService GetAccountService(EdoContext context)
        {
            var entityLockerMock = new Mock<IEntityLocker>();

            entityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(Result.Success()));

            return new CounterpartyAccountService(context, entityLockerMock.Object, Mock.Of<IAccountBalanceAuditService>(),
                Mock.Of<ICounterpartyBillingNotificationService>());
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
                        Currency = Currencies.USD,
                        CounterpartyId = 2,
                        IsActive = true
                    }
                }));

            edoContextMock.Setup(c => c.AccountBalanceAuditLogs).Returns(DbSetMockProvider.GetDbSetMock(new List<AccountBalanceAuditLogEntry>()));

            return edoContextMock.Object;
        }


        private readonly ApiCaller _apiCaller = new ApiCaller(1, ApiCallerTypes.Admin);
    }
}