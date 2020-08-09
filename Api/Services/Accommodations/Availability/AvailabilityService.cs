using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AvailabilityService : IAvailabilityService
    {
        public AvailabilityService(IAgentContextService agentContextService,
            IPriceProcessor priceProcessor,
            IAvailabilityResultsCache availabilityResultsCache,
            IAvailabilityStorage availabilityStorage,
            IProviderRouter providerRouter)
        {
            _agentContextService = agentContextService;
            _priceProcessor = priceProcessor;
            _availabilityResultsCache = availabilityResultsCache;
            _availabilityStorage = availabilityStorage;
            _providerRouter = providerRouter;
        }


        public async Task<Result<ProviderData<SingleAccommodationAvailabilityDetails>, ProblemDetails>> GetAvailable(DataProviders dataProvider, Guid searchId,
            string accommodationId,
            string languageCode)
        {
            var agent = await _agentContextService.GetAgent();

            return await GetFromCache()
                .Bind(ConvertCurrencies)
                .Map(ApplyMarkups)
                .Map(AddProviderData);


            async Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetFromCache()
            {
                // TODO: Rewrite this method when output model will be changed:
                // 1. Remove getting accommodation
                // 2. Remove getting request data
                // 3. Get result from availability storage directly
                var (_, isGetRequestFailure, request, getRequestError) = await _availabilityStorage.GetRequest(searchId);
                if (isGetRequestFailure)
                    return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetails>(getRequestError);
                
                var availability = (await _availabilityStorage.GetResult(searchId, agent))
                    .SingleOrDefault(r => r.Source == dataProvider && r.Data.AccommodationDetails.Id == accommodationId);

                if (availability.Equals(default))
                    return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetails>("Could not find availability result");
                
                var (_, isGetAccommodationFailure, accommodationDetails, getAccommodationError) = await _providerRouter
                    .GetAccommodation(dataProvider, accommodationId, languageCode);

                if (isGetAccommodationFailure)
                    return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetails>($"Could not accommodation: {getAccommodationError.Detail}");

                return new SingleAccommodationAvailabilityDetails(availability.Data.AvailabilityId,
                    request.CheckInDate,
                    request.CheckOutDate,
                    (request.CheckOutDate - request.CheckInDate).Days,
                    accommodationDetails,
                    availability.Data.RoomContractSets);
            }


            Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> ConvertCurrencies(SingleAccommodationAvailabilityDetails availabilityDetails)
                => _priceProcessor.ConvertCurrencies(agent, availabilityDetails, AvailabilityResultsExtensions.ProcessPrices, AvailabilityResultsExtensions.GetCurrency);


            Task<DataWithMarkup<SingleAccommodationAvailabilityDetails>> ApplyMarkups(SingleAccommodationAvailabilityDetails response) 
                => _priceProcessor.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);


            ProviderData<SingleAccommodationAvailabilityDetails> AddProviderData(DataWithMarkup<SingleAccommodationAvailabilityDetails> availabilityDetails)
                => ProviderData.Create(dataProvider, availabilityDetails.Data);
        }


        public async Task<Result<ProviderData<SingleAccommodationAvailabilityDetailsWithDeadline?>, ProblemDetails>> GetExactAvailability(
            DataProviders dataProvider, string availabilityId, Guid roomContractSetId, string languageCode)
        {
            var agent = await _agentContextService.GetAgent();

            return await ExecuteRequest()
                .Bind(ConvertCurrencies)
                .Map(ApplyMarkups)
                .Tap(SaveToCache)
                .Map(AddProviderData);


            Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> ExecuteRequest()
                => _providerRouter.GetExactAvailability(dataProvider, availabilityId, roomContractSetId, languageCode);


            Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> ConvertCurrencies(SingleAccommodationAvailabilityDetailsWithDeadline? availabilityDetails) => _priceProcessor.ConvertCurrencies(agent,
                availabilityDetails,
                AvailabilityResultsExtensions.ProcessPrices,
                AvailabilityResultsExtensions.GetCurrency);


            Task<DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline?>>
                ApplyMarkups(SingleAccommodationAvailabilityDetailsWithDeadline? response)
                => _priceProcessor.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);


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
            DataProviders dataProvider, string availabilityId, Guid roomContractSetId, string languageCode)
        {
            return GetDeadline()
                .Map(AddProviderData);

            Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline() => _providerRouter.GetDeadline(dataProvider,
                availabilityId,
                roomContractSetId, languageCode);

            ProviderData<DeadlineDetails> AddProviderData(DeadlineDetails deadlineDetails)
                => ProviderData.Create(dataProvider, deadlineDetails);
        }


        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly IAvailabilityStorage _availabilityStorage;
        private readonly IAgentContextService _agentContextService;
        private readonly IProviderRouter _providerRouter;
        private readonly IPriceProcessor _priceProcessor;
    }
}