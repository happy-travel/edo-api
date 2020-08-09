using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AvailabilitySearchScheduler : IAvailabilitySearchScheduler
    {
        public AvailabilitySearchScheduler(IServiceScopeFactory serviceScopeFactory,
            IDataProviderFactory dataProviderFactory,
            ILocationService locationService,
            ILogger<AvailabilitySearchScheduler> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _dataProviderFactory = dataProviderFactory;
            _locationService = locationService;
            _logger = logger;
        }


        public async Task<Result<Guid>> StartSearch(AvailabilityRequest request, AgentContext agent, string languageCode)
        {
            var searchId = Guid.NewGuid();
            _logger.LogMultiProviderAvailabilitySearchStarted($"Starting availability search with id '{searchId}'");
            
            var (_, isFailure, location, locationError) = await _locationService.Get(request.Location, languageCode);
            if (isFailure)
                return Result.Failure<Guid>(locationError.Detail);

            StartSearchTasks(searchId, request, location, agent, languageCode);
            
            return Result.Ok(searchId);
        }


        private void StartSearchTasks(Guid searchId, AvailabilityRequest request, Location location, AgentContext agent, string languageCode)
        {
            var contractsRequest = ConvertRequest(request, location);

            foreach (var provider in GetProviders(location))
            {
                Task.Run(() => StartProviderSearch(searchId,
                    contractsRequest,
                    agent,
                    languageCode,
                    provider.Key,
                    provider.Provider));
            }
            
            Task.Run(() => SaveRequest(searchId, request));


            IReadOnlyCollection<(DataProviders Key, IDataProvider Provider)> GetProviders(in Location location)
            {
                var providers = location.DataProviders != null && location.DataProviders.Any()
                    ? _dataProviderFactory.Get(location.DataProviders)
                    : _dataProviderFactory.GetAll();
                
                return providers;
            }


            static EdoContracts.Accommodations.AvailabilityRequest ConvertRequest(in AvailabilityRequest request, in Location location)
            {
                var roomDetails = request.RoomDetails
                    .Select(r => new RoomOccupationRequest(r.AdultsNumber, r.ChildrenAges, r.Type, r.IsExtraBedNeeded))
                    .ToList();

                return new EdoContracts.Accommodations.AvailabilityRequest(request.Nationality, request.Residency, request.CheckInDate,
                    request.CheckOutDate,
                    request.Filters, roomDetails,
                    new EdoContracts.GeoData.Location(location.Name, location.Locality, location.Country, location.Coordinates, location.Distance,
                        location.Source, location.Type),
                    request.PropertyType, request.Ratings);
            }
        }


        private async Task StartProviderSearch(Guid searchId, EdoContracts.Accommodations.AvailabilityRequest request, AgentContext agent, string languageCode,
            DataProviders providerKey, IDataProvider dataProvider)
        {
            // This task usually finishes later than outer scope of this service is disposed.
            // Creating new scope helps to avoid early dependencies disposal
            // https://docs.microsoft.com/ru-ru/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.1#do-not-capture-services-injected-into-the-controllers-on-background-threads
            using var serviceScope = _serviceScopeFactory.CreateScope();
            var storage = serviceScope.ServiceProvider.GetRequiredService<IAvailabilityStorage>();
            var priceProcessor = serviceScope.ServiceProvider.GetRequiredService<IPriceProcessor>();

            try
            {
                _logger.LogProviderAvailabilitySearchStarted($"Availability search with id '{searchId}' on provider '{providerKey}' started");

                await GetAvailability(request, languageCode)
                    .Bind(ConvertCurrencies)
                    .Map(ApplyMarkups)
                    .Tap(SaveResults)
                    .Finally(SaveState);
            }
            catch (Exception ex)
            {
                // TODO: Add sentry error notification
                _logger.LogProviderAvailabilitySearchException(ex);
                var result = ProblemDetailsBuilder.Fail<AvailabilityDetails>("Server error", HttpStatusCode.InternalServerError);
                await SaveState(result);
            }
            

            async Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(EdoContracts.Accommodations.AvailabilityRequest request,
                string languageCode)
            {
                var saveToStorageTask = storage.SetState(searchId, providerKey, AvailabilitySearchState.Pending(searchId));
                var getAvailabilityTask = dataProvider.GetAvailability(request, languageCode);
                await Task.WhenAll(saveToStorageTask, getAvailabilityTask);

                return getAvailabilityTask.Result;
            }


            Task<Result<AvailabilityDetails, ProblemDetails>> ConvertCurrencies(AvailabilityDetails availabilityDetails)
                => priceProcessor.ConvertCurrencies(agent, availabilityDetails, AvailabilityResultsExtensions.ProcessPrices,
                    AvailabilityResultsExtensions.GetCurrency);


            async Task<AvailabilityDetails> ApplyMarkups(AvailabilityDetails response)
            {
                var markup = await priceProcessor.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);
                return markup.Data;
            }


            Task SaveResults(AvailabilityDetails details) => storage.SaveResult(searchId, providerKey, details);


            Task SaveState(Result<AvailabilityDetails, ProblemDetails> result)
            {
                var state = result.IsSuccess
                    ? AvailabilitySearchState.Completed(searchId, result.Value.Results.Count)
                    : AvailabilitySearchState.Failed(searchId, result.Error.Detail);

                if (state.TaskState == AvailabilitySearchTaskState.Completed)
                {
                    _logger.LogProviderAvailabilitySearchSuccess($"Availability search with id '{searchId}' on provider '{providerKey}' finished successfully with '{state.ResultCount}' results");
                }
                else
                {
                    _logger.LogProviderAvailabilitySearchFailure($"Availability search with id '{searchId}' on provider '{providerKey}' finished with state '{state.TaskState}', error '{state.Error}'");
                }
                

                return storage.SetState(searchId, providerKey, state);
            }
        }
        
        
        private Task SaveRequest(Guid searchId, in AvailabilityRequest request)
        {
            // This task usually finishes later than outer scope of this service is disposed.
            // Creating new scope helps to avoid early dependencies disposal
            // https://docs.microsoft.com/ru-ru/aspnet/core/performance/performance-best-practices?view=aspnetcore-3.1#do-not-capture-services-injected-into-the-controllers-on-background-threads
            using var serviceScope = _serviceScopeFactory.CreateScope();
            var storage = serviceScope.ServiceProvider.GetRequiredService<IAvailabilityStorage>();

            return storage.SaveRequest(searchId, request);
        }


        private readonly IDataProviderFactory _dataProviderFactory;


        private readonly ILocationService _locationService;
        private readonly ILogger<AvailabilitySearchScheduler> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
    }
}