using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.CurrencyConverter;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Helpers;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.CurrencyConversion
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        public CurrencyConverterService(ICurrencyRateService rateService, ICurrencyConverterFactory converterFactory)
        {
            _converterFactory = converterFactory;
            _rateService = rateService;
        }


        public async Task<Result<TData>> ConvertPricesInData<TData>(AgentContext agent, TData data,
            Func<TData, PriceProcessFunction, ValueTask<TData>> changePricesFunc, Func<TData, Currencies?> getCurrencyFunc)
        {
            var currentCurrency = getCurrencyFunc(data);
            if (!currentCurrency.HasValue)
                return Result.Success(data);
            
            if (currentCurrency == TargetCurrency)
                return Result.Success(data);
                
            if (currentCurrency == Currencies.NotSpecified)
                return Result.Failure<TData>($"Cannot convert from '{Currencies.NotSpecified}' currency");
            
            var (_, isFailure, rate, error) = await _rateService.Get(currentCurrency.Value, TargetCurrency);
            if (isFailure)
                return Result.Failure<TData>(error);

            var converter = _converterFactory.Create(in rate, currentCurrency.Value, TargetCurrency);
            var convertedDetails = await changePricesFunc(data, price =>
            {
                var convertedAmount = converter.Convert(price);
                var ceiledAmount = MoneyRounder.Ceil(convertedAmount);

                return new ValueTask<MoneyAmount>(ceiledAmount);
            });
            
            return Result.Success(convertedDetails);
        }


        // Only USD Currency is supported for now.
        public const Currencies TargetCurrency = Currencies.USD;

        private readonly ICurrencyConverterFactory _converterFactory;
        private readonly ICurrencyRateService _rateService;
    }
}