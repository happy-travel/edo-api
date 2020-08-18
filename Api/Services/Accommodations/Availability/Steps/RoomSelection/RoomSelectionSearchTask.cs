using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionSearchTask
    {
        private RoomSelectionSearchTask(IPriceProcessor priceProcessor,
            IDataProviderFactory dataProviderFactory,
            IRoomSelectionStorage roomSelectionStorage)
        {
            _priceProcessor = priceProcessor;
            _dataProviderFactory = dataProviderFactory;
            _roomSelectionStorage = roomSelectionStorage;
        }


        public static RoomSelectionSearchTask Create(IServiceProvider serviceProvider)
        {
            return new RoomSelectionSearchTask(
                serviceProvider.GetRequiredService<IPriceProcessor>(),
                serviceProvider.GetRequiredService<IDataProviderFactory>(),
                serviceProvider.GetRequiredService<IRoomSelectionStorage>()
            );
        }
        
        
        public async Task<Result<ProviderData<SingleAccommodationAvailabilityDetails>, ProblemDetails>> GetProviderAvailability(Guid searchId,
            Guid resultId,
            DataProviders dataProvider,
            string accommodationId, string availabilityId, AgentContext agent,
            string languageCode)
        {
            return await ExecuteRequest()
                .Bind(ConvertCurrencies)
                .Map(ApplyMarkups)
                .Map(AddProviderData)
                .Tap(SaveToCache);


            Task SaveToCache(ProviderData<SingleAccommodationAvailabilityDetails> details) => _roomSelectionStorage.SaveResult(searchId, resultId, details.Data, details.Source);


            Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> ExecuteRequest()
                => _dataProviderFactory.Get(dataProvider).GetAvailability(availabilityId, accommodationId, languageCode);


            Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> ConvertCurrencies(SingleAccommodationAvailabilityDetails availabilityDetails)
                => _priceProcessor.ConvertCurrencies(agent, availabilityDetails, AvailabilityResultsExtensions.ProcessPrices, AvailabilityResultsExtensions.GetCurrency);


            Task<DataWithMarkup<SingleAccommodationAvailabilityDetails>> ApplyMarkups(SingleAccommodationAvailabilityDetails response) 
                => _priceProcessor.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);


            ProviderData<SingleAccommodationAvailabilityDetails> AddProviderData(DataWithMarkup<SingleAccommodationAvailabilityDetails> availabilityDetails)
                => ProviderData.Create(dataProvider, availabilityDetails.Data);
        }
        
        
        private readonly IPriceProcessor _priceProcessor;
        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly IRoomSelectionStorage _roomSelectionStorage;
    }
}