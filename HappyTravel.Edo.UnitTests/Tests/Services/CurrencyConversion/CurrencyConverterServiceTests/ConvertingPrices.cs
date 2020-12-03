using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Money.Enums;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.CurrencyConversion.CurrencyConverterServiceTests
{
    public class ConvertingPrices
    {
        [Fact]
        public async Task Conversion_should_returns_success_when_a_current_currency_is_null()
        {
            var getCurrencyFunc = new Func<object, Currencies?>(o => null);
            var service = new CurrencyConverterService(null, null, null, null);

            var result = await service.ConvertPricesInData(new AgentContext(), new object(), null, getCurrencyFunc);

            Assert.True(result.IsSuccess);
        }


        [Fact]
        public async Task Conversion_should_returns_success_when_a_current_currency_equals_a_target_currency()
        {
            var getCurrencyFunc = new Func<object, Currencies?>(o => CurrencyConverterService.TargetCurrency);
            var service = new CurrencyConverterService(null, null, null, null);

            var result = await service.ConvertPricesInData(new AgentContext(), new object(), null, getCurrencyFunc);

            Assert.True(result.IsSuccess);
        }


        [Fact]
        public async Task Conversion_should_returns_failure_when_a_current_currency_is_not_specified()
        {
            var getCurrencyFunc = new Func<object, Currencies?>(o => Currencies.NotSpecified);
            var service = new CurrencyConverterService(null, null, null, null);

            var result = await service.ConvertPricesInData(new AgentContext(), new object(), null, getCurrencyFunc);

            Assert.True(result.IsFailure);
        }


        [Fact]
        public async Task Conversion_should_returns_failure_when_a_rate_currency_service_returns_failure()
        {
            var rateServiceMock = new Mock<ICurrencyRateService>();
            rateServiceMock.Setup(s => s.Get(It.IsAny<Currencies>(), It.IsAny<Currencies>()))
                .Returns(new ValueTask<Result<decimal>>(Result.Failure<decimal>("error")));
            var service = new CurrencyConverterService(rateServiceMock.Object, null, null, null);

            var result = await service.ConvertPricesInData(new AgentContext(), new object(), null, GetCurrencyFunc);

            Assert.True(result.IsFailure);
        }


        [Fact(Skip = "Can't test, because of ICurrencyConverterFactory design")]
        public void Conversion_should_returns_success()
        { }


        private static Func<object, Currencies?> GetCurrencyFunc => _ => Currencies.EUR;
    }
}
