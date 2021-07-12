using System;
using System.Globalization;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupService : IMarkupService
    {
        public MarkupService(IMarkupPolicyService markupPolicyService,
            IDiscountFunctionService discountFunctionService,
            IMarkupPolicyTemplateService templateService,
            ICurrencyRateService currencyRateService,
            IMemoryFlow flow)
        {
            _markupPolicyService = markupPolicyService;
            _discountFunctionService = discountFunctionService;
            _templateService = templateService;
            _currencyRateService = currencyRateService;
            _flow = flow;
        }
        
        
        public async Task<TDetails> ApplyMarkups<TDetails>(AgentContext agent, TDetails details,
            Func<TDetails, PriceProcessFunction, ValueTask<TDetails>> priceProcessFunc, 
            Action<MarkupApplicationResult<TDetails>> logAction = null)
        {
            var policies = await _markupPolicyService.Get(agent, MarkupPolicyTarget.AccommodationAvailability);
            var currentData = details;
            foreach (var policy in policies)
            {
                var detailsBefore = currentData;
                
                var markupFunction = GetPriceProcessFunction(policy);
                currentData = await priceProcessFunc(currentData, markupFunction);

                var discountFunction = await _discountFunctionService.Get(policy, agent);
                currentData = await priceProcessFunc(currentData, discountFunction);;

                logAction?.Invoke(new MarkupApplicationResult<TDetails>(detailsBefore, policy, currentData));
            }
            
            return currentData;
        }


        private PriceProcessFunction GetPriceProcessFunction(MarkupPolicy policy)
        {
            var policyFunction = GetPolicyFunction(policy);
            return async initialPrice =>
            {
                var amount = initialPrice.Amount;
                var (_, _, currencyRate, _) = await _currencyRateService.Get(initialPrice.Currency, policyFunction.Currency);
                amount = policyFunction.Function(amount * currencyRate) / currencyRate;
                return new MoneyAmount(amount, initialPrice.Currency);
            };
        }


        private MarkupPolicyFunction GetPolicyFunction(MarkupPolicy policy)
        {
            return _flow
                .GetOrSet(BuildKey(policy),
                    () =>
                    {
                        return new MarkupPolicyFunction
                        {
                            Currency = policy.Currency,
                            Function = _templateService
                                .CreateFunction(policy.TemplateId, policy.TemplateSettings)
                        };
                    },
                    MarkupPolicyFunctionCachingTime);


            string BuildKey(MarkupPolicy policyWithFunc)
                => _flow.BuildKey(nameof(MarkupPolicyService),
                    nameof(GetPolicyFunction),
                    policyWithFunc.Id.ToString(),
                    policyWithFunc.Modified.ToString(CultureInfo.InvariantCulture));
        }
        
        private readonly IMarkupPolicyTemplateService _templateService;
        private readonly ICurrencyRateService _currencyRateService;
        private static readonly TimeSpan MarkupPolicyFunctionCachingTime = TimeSpan.FromDays(1);
        
        private readonly IMarkupPolicyService _markupPolicyService;
        private readonly IDiscountFunctionService _discountFunctionService;
        private readonly IMemoryFlow _flow;
    }
}