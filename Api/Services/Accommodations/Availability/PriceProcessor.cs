using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Money.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class PriceProcessor : IPriceProcessor
    {
        public PriceProcessor(IMarkupService markupService, ICurrencyConverterService currencyConverter)
        {
            _markupService = markupService;
            _currencyConverter = currencyConverter;
        }


        public Task<Result<TDetails, ProblemDetails>> ConvertCurrencies<TDetails>(AgentContext agent, TDetails details,
            Func<TDetails, PriceProcessFunction, ValueTask<TDetails>> changePricesFunc, Func<TDetails, Currencies?> getCurrencyFunc)
        {
            return _currencyConverter
                .ConvertPricesInData(agent, details, changePricesFunc, getCurrencyFunc)
                .ToResultWithProblemDetails();
        }


        public Task<TDetails> ApplyMarkups<TDetails>(AgentContext agent, TDetails details,
            Func<TDetails, PriceProcessFunction, ValueTask<TDetails>> priceProcessFunc,
            Action<MarkupApplicationResult<TDetails>> logAction = null)
            => _markupService.ApplyMarkups(agent, details, priceProcessFunc, logAction);


        private readonly IMarkupService _markupService;
        private readonly ICurrencyConverterService _currencyConverter;
    }
}