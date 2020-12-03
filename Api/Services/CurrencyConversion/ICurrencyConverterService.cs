using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Services.CurrencyConversion
{
    public interface ICurrencyConverterService
    {
        Task<Result<TData>> ConvertPricesInData<TData>(AgentContext agent, TData data,
            Func<TData, PriceProcessFunction, ValueTask<TData>> changePricesFunc, Func<TData, Currencies?> getCurrencyFunc);


        Task<Result<decimal>> Convert(Currencies source, Currencies target, decimal value);
    }
}