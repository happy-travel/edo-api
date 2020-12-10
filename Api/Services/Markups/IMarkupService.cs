using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.PriceProcessing;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IMarkupService
    {
        /// <summary>
        /// Applies markups to given data
        /// </summary>
        /// <param name="agent">Agent</param>
        /// <param name="details">Data to apply markups</param>
        /// <param name="priceProcessFunc">Function to change prices in Data</param>
        /// <param name="logAction">Action to execute after each applied markup policy</param>
        /// <typeparam name="TDetails">Data type to apply markups</typeparam>
        /// <returns>Resulting data with applied markups</returns>
        Task<TDetails> ApplyMarkups<TDetails>(AgentContext agent, TDetails details,
            Func<TDetails, PriceProcessFunction, ValueTask<TDetails>> priceProcessFunc, Action<MarkupApplicationResult<TDetails>> logAction = null);
    }
}