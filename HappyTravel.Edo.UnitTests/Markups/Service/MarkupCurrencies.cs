using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Edo.UnitTests.Infrastructure;
using HappyTravel.Edo.UnitTests.Infrastructure.DbSetMocks;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Markups.Service
{
    public class MarkupCurrencies
    {
        public MarkupCurrencies(Mock<EdoContext> edoContextMock, IMemoryFlow memoryFlow)
        {
            edoContextMock.Setup(c => c.MarkupPolicies)
                .Returns(DbSetMockProvider.GetDbSetMock(PoliciesWithCurrency));

            _edoContext = edoContextMock.Object;
            _memoryFlow = memoryFlow;

            var customerSettingsMock = new Mock<ICustomerSettingsManager>();
            customerSettingsMock
                .Setup(s => s.GetUserSettings(It.IsAny<CustomerInfo>()))
                .Returns(Task.FromResult(Result.Ok(new CustomerUserSettings(false, It.IsAny<Currencies>()))));

            _customerSettings = customerSettingsMock.Object;
        }


        [Fact]
        public async Task Markup_calculation_should_call_currency_service()
        {
            var currencyRateServiceMock = CreateSimpleRateService();
            var markupFunction = await GetMarkupFunctionWithCurrency(currencyRateServiceMock.Object);
            await markupFunction(100, Currencies.EUR);

            currencyRateServiceMock
                .Verify(r => r.Get(Currencies.EUR, Currencies.USD), Times.Once);

            currencyRateServiceMock
                .Verify(r => r.Get(Currencies.EUR, Currencies.EUR), Times.Once);

            Mock<ICurrencyRateService> CreateSimpleRateService()
            {
                var serviceMock = new Mock<ICurrencyRateService>();
                serviceMock
                    .Setup(c => c.Get(It.IsAny<Currencies>(), It.IsAny<Currencies>()))
                    .Returns(new ValueTask<decimal>(1));

                return serviceMock;
            }
        }


        [Theory]
        [InlineData(100, 139.6)]
        [InlineData(24.5, 42.507)]
        public async Task Markup_should_calculate_with_currency_rate(decimal supplierPrice, decimal expectedResultPrice)
        {
            var currencyRateServiceMock = CreateRateService();
            var markupFunction = await GetMarkupFunctionWithCurrency(currencyRateServiceMock.Object);
            var resultPrice = await markupFunction(supplierPrice, Currencies.EUR);
            Assert.Equal(expectedResultPrice, resultPrice);

            Mock<ICurrencyRateService> CreateRateService()
            {
                var serviceMock = new Mock<ICurrencyRateService>();
                serviceMock
                    .Setup(c => c.Get(Currencies.EUR, Currencies.USD))
                    .Returns(new ValueTask<decimal>((decimal)1.2));
                
                serviceMock
                    .Setup(c => c.Get(Currencies.EUR, Currencies.EUR))
                    .Returns(new ValueTask<decimal>(1));

                return serviceMock;
            }
        }


        private async Task<AggregatedMarkupFunction> GetMarkupFunctionWithCurrency(ICurrencyRateService currencyRateService)
        {
            var markupService = new MarkupService(_edoContext,
                _memoryFlow,
                _templateService,
                currencyRateService,
                _customerSettings);
            
            var markup = await markupService.Get(_customerInfo, PolicyTarget);
            return markup.Function;
        }


        private static readonly MarkupPolicy[] PoliciesWithCurrency = new[]
        {
            new MarkupPolicy
            {
                Order = 1,
                Target = PolicyTarget,
                ScopeType = MarkupPolicyScopeType.Global,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", (decimal) 1.286}},
                Currency = Currencies.EUR
            },
            new MarkupPolicy
            {
                Order = 2,
                Target = PolicyTarget,
                ScopeType = MarkupPolicyScopeType.Global,
                TemplateId = 2,
                TemplateSettings = new Dictionary<string, decimal> {{"addition", (decimal) 13.2}},
                Currency = Currencies.USD
            }
        };

        private const MarkupPolicyTarget PolicyTarget = MarkupPolicyTarget.AccommodationAvailability;
        private readonly EdoContext _edoContext;
        private readonly IMemoryFlow _memoryFlow;
        private readonly ICustomerSettingsManager _customerSettings;
        private readonly CustomerInfo _customerInfo = CustomerInfoFactory.GetByCustomerId(1);
        private readonly IMarkupPolicyTemplateService _templateService = new MarkupPolicyTemplateService();
    }
}