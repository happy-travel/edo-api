using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Helpers;

namespace HappyTravel.Edo.Api.Services.CurrencyConversion
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        public CurrencyConverterService(ICurrencyRateService rateService,
            IAgentSettingsManager agentSettingsManager,
            ICounterpartyService counterpartyService,
            IMemoryFlow memoryFlow)
        {
            _rateService = rateService;
            _agentSettingsManager = agentSettingsManager;
            _counterpartyService = counterpartyService;
            _memoryFlow = memoryFlow;
        }


        public async Task<Result<TData>> ConvertPricesInData<TData>(AgentInfo agent, TData data,
            Func<TData, PriceProcessFunction, ValueTask<TData>> changePricesFunc, Func<TData, Currencies?> getCurrencyFunc)
        {
            var currentCurrency = getCurrencyFunc(data);
            if(!currentCurrency.HasValue)
                return Result.Ok(data);
                
            if (currentCurrency == Currencies.NotSpecified)
                return Result.Fail<TData>($"Cannot convert from '{Currencies.NotSpecified}' currency");
            
            var targetCurrency = await GetTargetCurrency(agent);
            if (targetCurrency == Currencies.NotSpecified)
                return Result.Fail<TData>($"Cannot convert to '{Currencies.NotSpecified}' currency");

            if (targetCurrency == currentCurrency)
                return Result.Ok(data);
            
            var (_, isFailure, rate, error) = await _rateService.Get(currentCurrency.Value, targetCurrency);
            if (isFailure)
                return Result.Fail<TData>(error);

            var convertedDetails = await changePricesFunc(data, (price, currency) =>
            {
                var newCurrency = targetCurrency;
                var newPrice = MoneyCeiler.Ceil(price * rate, newCurrency);

                return new ValueTask<(decimal, Currencies)>((newPrice, newCurrency));
            });
            
            return Result.Ok(convertedDetails);
            
            ValueTask<Currencies> GetTargetCurrency(AgentInfo agentInfo)
            {
                return new ValueTask<Currencies>(Currencies.USD);
                var key = _memoryFlow.BuildKey(nameof(CurrencyConverterService), "TARGET_CURRENCY", agentInfo.AgentId.ToString());
                return _memoryFlow.GetOrSetAsync(key, async () =>
                {
                    var settings = await _agentSettingsManager.GetUserSettings(agentInfo);
                    if (settings.DisplayCurrency != Currencies.NotSpecified)
                        return settings.DisplayCurrency;

                    var (_, _, counterparty, _) = await _counterpartyService.Get(agentInfo.CounterpartyId);
                    return counterparty.PreferredCurrency;
                }, TargetCurrencyCacheLifeTime);
            }
        }


        private static readonly TimeSpan TargetCurrencyCacheLifeTime = TimeSpan.FromMinutes(10);

        private readonly ICurrencyRateService _rateService;
        private readonly IAgentSettingsManager _agentSettingsManager;
        private readonly ICounterpartyService _counterpartyService;
        private readonly IMemoryFlow _memoryFlow;
    }
}