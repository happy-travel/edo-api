using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Edo.UnitTests.Infrastructure;
using HappyTravel.Edo.UnitTests.Infrastructure.DbSetMocks;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Payments
{
    public class CanPayWithAccount
    {
        public CanPayWithAccount(Mock<EdoContext> edoContextMock, IDateTimeProvider dateTimeProvider)
        {
            _paymentService = new PaymentService(Mock.Of<IAdministratorContext>(), Mock.Of<IPaymentProcessingService>(), edoContextMock.Object,
                Mock.Of<IPayfortService>(), dateTimeProvider);

            edoContextMock
                .Setup(c => c.PaymentAccounts)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<PaymentAccount>
                {
                    new PaymentAccount
                    {
                        Id = 1,
                        Balance = 0,
                        Currency = Currencies.USD,
                        CompanyId = 1,
                        CreditLimit = 0
                    },
                    new PaymentAccount
                    {
                        Id = 3,
                        Balance = 5,
                        Currency = Currencies.USD,
                        CompanyId = 3,
                        CreditLimit = 0
                    },
                    new PaymentAccount
                    {
                        Id = 4,
                        Balance = 0,
                        Currency = Currencies.USD,
                        CompanyId = 4,
                        CreditLimit = 3
                    }
                }));
        }

        [Fact]
        public async Task Invalid_payment_without_account_should_be_permitted()
        {
            var canPay = await _paymentService.CanPayWithAccount(_invalidCustomerInfo);
            Assert.False(canPay);
        }

        [Fact]
        public async Task Invalid_cannot_pay_with_account_if_balance_zero()
        {
            var canPay = await _paymentService.CanPayWithAccount(_validCustomerInfo);
            Assert.False(canPay);
        }

        [Fact]
        public async Task Valid_can_pay_if_balance_greater_zero()
        {
            var canPay = await _paymentService.CanPayWithAccount(_validCustomerInfoWithPositiveBalance);
            Assert.True(canPay);
        }

        [Fact]
        public async Task Valid_can_pay_if_credit_greater_zero()
        {
            var canPay = await _paymentService.CanPayWithAccount(_validCustomerInfoWithPositiveCredit);
            Assert.True(canPay);
        }


        private readonly CustomerInfo _validCustomerInfo = CustomerInfoFactory.GetByWithCompanyAndBranch(1, 1, 1);
        private readonly CustomerInfo _invalidCustomerInfo = CustomerInfoFactory.GetByWithCompanyAndBranch(2, 2, 2);
        private readonly CustomerInfo _validCustomerInfoWithPositiveBalance = CustomerInfoFactory.GetByWithCompanyAndBranch(3, 3, 3);
        private readonly CustomerInfo _validCustomerInfoWithPositiveCredit = CustomerInfoFactory.GetByWithCompanyAndBranch(4, 4, 4);
        private readonly IPaymentService _paymentService;
    }
}
