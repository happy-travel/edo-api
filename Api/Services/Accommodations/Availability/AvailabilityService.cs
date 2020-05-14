using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.GeoData;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Helpers;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AvailabilityService : IAvailabilityService
    {
        public AvailabilityService(ILocationService locationService,
            IAgentContext agentContext,
            IMarkupService markupService,
            IAvailabilityResultsCache availabilityResultsCache,
            IProviderRouter providerRouter,
            ICurrencyConverterService currencyConverterService)
        {
            _locationService = locationService;
            _agentContext = agentContext;
            _markupService = markupService;
            _availabilityResultsCache = availabilityResultsCache;
            _providerRouter = providerRouter;
            _currencyConverterService = currencyConverterService;
        }


        public async ValueTask<Result<CombinedAvailabilityDetails, ProblemDetails>> GetAvailable(AvailabilityRequest request, AgentInfo agent,
            RequestMetadata requestMetadata)
        {
            var (_, isFailure, location, error) = await _locationService.Get(request.Location, requestMetadata.LanguageCode);
            if (isFailure)
                return Result.Fail<CombinedAvailabilityDetails, ProblemDetails>(error);

            return await ExecuteRequest()
                .OnSuccess(ConvertCurrencies)
                .OnSuccess(ApplyMarkups)
                .OnSuccess(ReturnResponseWithMarkup);


            Task<Result<CombinedAvailabilityDetails, ProblemDetails>> ExecuteRequest()
            {
                var roomDetails = request.RoomDetails
                    .Select(r => new RoomOccupationRequest(r.AdultsNumber, r.ChildrenAges, r.Type,
                        r.IsExtraBedNeeded))
                    .ToList();

                var contract = new EdoContracts.Accommodations.AvailabilityRequest(request.Nationality, request.Residency, request.CheckInDate,
                    request.CheckOutDate,
                    request.Filters, roomDetails,
                    new Location(location.Name, location.Locality, location.Country, location.Coordinates, location.Distance, location.Source, location.Type),
                    request.PropertyType, request.Ratings);

                return _providerRouter.GetAvailability(location.DataProviders, contract, requestMetadata)
                    .ToResultWithProblemDetails();
            }


            Task<Result<CombinedAvailabilityDetails, ProblemDetails>> ConvertCurrencies(CombinedAvailabilityDetails availabilityDetails)
                => this.ConvertCurrencies(agent, availabilityDetails, AvailabilityResultsExtensions.ProcessPrices, AvailabilityResultsExtensions.GetCurrency);


            Task<DataWithMarkup<CombinedAvailabilityDetails>> ApplyMarkups(CombinedAvailabilityDetails response) 
                => this.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);

            
            CombinedAvailabilityDetails ReturnResponseWithMarkup(DataWithMarkup<CombinedAvailabilityDetails> markup) => markup.Data;
        }


        public async Task<Result<ProviderData<SingleAccommodationAvailabilityDetails>, ProblemDetails>> GetAvailable(DataProviders dataProvider,
            string accommodationId, string availabilityId,
            RequestMetadata requestMetadata)
        {
            var agent = await _agentContext.GetAgent();

            return await ExecuteRequest()
                .OnSuccess(ConvertCurrencies)
                .OnSuccess(ApplyMarkups)
                .OnSuccess(AddProviderData);


            Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> ExecuteRequest()
                => _providerRouter.GetAvailable(dataProvider, accommodationId, availabilityId, requestMetadata);


            Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> ConvertCurrencies(SingleAccommodationAvailabilityDetails availabilityDetails)
                => this.ConvertCurrencies(agent, availabilityDetails, AvailabilityResultsExtensions.ProcessPrices, AvailabilityResultsExtensions.GetCurrency);


            Task<DataWithMarkup<SingleAccommodationAvailabilityDetails>> ApplyMarkups(SingleAccommodationAvailabilityDetails response) 
                => this.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);


            ProviderData<SingleAccommodationAvailabilityDetails> AddProviderData(DataWithMarkup<SingleAccommodationAvailabilityDetails> availabilityDetails)
                => ProviderData.Create(dataProvider, availabilityDetails.Data);
        }


        public async Task<Result<ProviderData<SingleAccommodationAvailabilityDetailsWithDeadline?>, ProblemDetails>> GetExactAvailability(
            DataProviders dataProvider, string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata)
        {
            var agent = await _agentContext.GetAgent();

            return await ExecuteRequest()
                .OnSuccess(ConvertCurrencies)
                .OnSuccess(ApplyMarkups)
                .OnSuccess(SaveToCache)
                .OnSuccess(AddProviderData);


            Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> ExecuteRequest()
                => _providerRouter.GetExactAvailability(dataProvider, availabilityId, roomContractSetId, requestMetadata);


            Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> ConvertCurrencies(SingleAccommodationAvailabilityDetailsWithDeadline? availabilityDetails) => this.ConvertCurrencies(agent,
                availabilityDetails,
                AvailabilityResultsExtensions.ProcessPrices,
                AvailabilityResultsExtensions.GetCurrency);


            Task<DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline?>>
                ApplyMarkups(SingleAccommodationAvailabilityDetailsWithDeadline? response)
                => this.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);


            Task SaveToCache(DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline?> responseWithDeadline)
            {
                if(!responseWithDeadline.Data.HasValue)
                    return Task.CompletedTask;
                
                return _availabilityResultsCache.Set(dataProvider, DataWithMarkup.Create(responseWithDeadline.Data.Value, 
                    responseWithDeadline.Policies));
            }


            ProviderData<SingleAccommodationAvailabilityDetailsWithDeadline?> AddProviderData(
                DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline?> availabilityDetails)
                => ProviderData.Create(dataProvider, availabilityDetails.Data);
        }


        public Task<Result<ProviderData<DeadlineDetails>, ProblemDetails>> GetDeadlineDetails(
            DataProviders dataProvider, string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata)
        {
            return GetDeadline()
                .OnSuccess(AddProviderData);

            Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline() => _providerRouter.GetDeadline(dataProvider,
                availabilityId,
                roomContractSetId, requestMetadata);

            ProviderData<DeadlineDetails> AddProviderData(DeadlineDetails deadlineDetails)
                => ProviderData.Create(dataProvider, deadlineDetails);
        }
        
        
        private Task<Result<TDetails, ProblemDetails>> ConvertCurrencies<TDetails>(AgentInfo agent, TDetails details, Func<TDetails, PriceProcessFunction, ValueTask<TDetails>> changePricesFunc, Func<TDetails, Currencies?> getCurrencyFunc)
        {
            return _currencyConverterService
                .ConvertPricesInData(agent, details, changePricesFunc, getCurrencyFunc)
                .ToResultWithProblemDetails();
        }


        private async Task<DataWithMarkup<TDetails>> ApplyMarkups<TDetails>(AgentInfo agent, TDetails details,
            Func<TDetails, PriceProcessFunction, ValueTask<TDetails>> priceProcessFunc)
        {
            var markup = await _markupService.Get(agent, MarkupPolicyTarget.AccommodationAvailability);
            var responseWithMarkup = await priceProcessFunc(details, markup.Function);
            var ceiledResponse =  await priceProcessFunc(responseWithMarkup, (price, currency) =>
            {
                // TODO: Replace currency.ToString() with 'Currencies' from Money library
                var roundedPrice = MoneyCeiler.Ceil(price, currency);
                return new ValueTask<(decimal, Currencies)>((roundedPrice, currency));
            });
            
            return DataWithMarkup.Create(ceiledResponse, markup.Policies);
        }


        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly ICurrencyConverterService _currencyConverterService;
        private readonly IAgentContext _agentContext;
        private readonly ILocationService _locationService;
        private readonly IMarkupService _markupService;
        private readonly IProviderRouter _providerRouter;
    }
}