using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Metrics;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Availabilities.Mapping;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Mapping;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Hubs.Search;
using HappyTravel.Edo.Data.AccommodationMappings;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.SuppliersCatalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;
using AvailabilityRequest = HappyTravel.EdoContracts.Accommodations.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilitySearchTask
    {
        private WideAvailabilitySearchTask(IWideAvailabilityStorage storage, IWideAvailabilityPriceProcessor priceProcessor,
            ISupplierConnectorManager supplierConnectorManager, IDateTimeProvider dateTimeProvider,
            ILogger<WideAvailabilitySearchTask> logger, IHubContext<SearchHub, ISearchHub> hubContext,
            IAccommodationMapperClient mapperClient)
        {
            _storage = storage;
            _priceProcessor = priceProcessor;
            _supplierConnectorManager = supplierConnectorManager;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _hubContext = hubContext;
            _mapperClient = mapperClient;
        }


        public static WideAvailabilitySearchTask Create(IServiceProvider serviceProvider)
            => new(storage: serviceProvider.GetRequiredService<IWideAvailabilityStorage>(),
                priceProcessor: serviceProvider.GetRequiredService<IWideAvailabilityPriceProcessor>(),
                supplierConnectorManager: serviceProvider.GetRequiredService<ISupplierConnectorManager>(),
                dateTimeProvider: serviceProvider.GetRequiredService<IDateTimeProvider>(),
                logger: serviceProvider.GetRequiredService<ILogger<WideAvailabilitySearchTask>>(),
                hubContext: serviceProvider.GetRequiredService<IHubContext<SearchHub, ISearchHub>>(),
                mapperClient: serviceProvider.GetRequiredService<IAccommodationMapperClient>());


        public async Task Start(Guid searchId, Models.Availabilities.AvailabilityRequest availabilityRequest, 
            List<SupplierCodeMapping> accommodationCodeMappings, Suppliers supplier, 
            AgentContext agent, string languageCode,
            AccommodationBookingSettings searchSettings)
        {
            var supplierConnector = _supplierConnectorManager.Get(supplier);
            var connectorRequest = CreateRequest(availabilityRequest, accommodationCodeMappings, searchSettings);
            using var _ = Counters.WideAccommodationAvailabilitySearchTaskDuration.WithLabels(supplier.ToString()).NewTimer();

            try
            {
                _logger.LogProviderAvailabilitySearchStarted(searchId, supplier);

                await GetAvailability(connectorRequest, languageCode)
                    .Bind(ConvertCurrencies)
                    .Map(ProcessPolicies)
                    .Map(ApplyMarkups)
                    .Map(AlignPrices)
                    .Map(Convert)
                    .Tap(SaveResult)
                    .Tap(NotifyClient)
                    .Finally(SaveState);
            }
            catch (Exception ex)
            {
                _logger.LogProviderAvailabilitySearchException(ex);
                var result = ProblemDetailsBuilder.Fail<List<AccommodationAvailabilityResult>>("Server error", HttpStatusCode.InternalServerError);
                await SaveState(result);
            }


            async Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> GetAvailability(AvailabilityRequest request,
                string languageCode)
            {
                var saveToStorageTask = _storage.SaveState(searchId, SupplierAvailabilitySearchState.Pending(searchId), supplier);
                var getAvailabilityTask = Task.Run(() =>
                {
                    using var timer = Counters.SupplierSearchResponseTimeDuration.WithLabels("wide_availability_request", supplier.ToString()).NewTimer();
                    return supplierConnector.GetAvailability(request, languageCode);
                });
                await Task.WhenAll(saveToStorageTask, getAvailabilityTask);

                return getAvailabilityTask.Result;
            }


            Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> ConvertCurrencies(EdoContracts.Accommodations.Availability availabilityDetails) 
                => _priceProcessor.ConvertCurrencies(availabilityDetails, agent);


            Task<EdoContracts.Accommodations.Availability> ApplyMarkups(EdoContracts.Accommodations.Availability response) 
                => _priceProcessor.ApplyMarkups(response, agent);
            
            
            async Task<EdoContracts.Accommodations.Availability> AlignPrices(EdoContracts.Accommodations.Availability response) 
                => await _priceProcessor.AlignPrices(response);


            EdoContracts.Accommodations.Availability ProcessPolicies(EdoContracts.Accommodations.Availability response) 
                => WideAvailabilityPolicyProcessor.Process(response, searchSettings.CancellationPolicyProcessSettings);


            List<AccommodationAvailabilityResult> Convert(EdoContracts.Accommodations.Availability details)
            {
                var htIdMapping = accommodationCodeMappings
                    .ToDictionary(m => m.SupplierCode, m => m.HtId);
                
                var timestamp = _dateTimeProvider.UtcNow().Ticks;
                return details
                    .Results
                    .Select(accommodationAvailability =>
                    {
                        var minPrice = accommodationAvailability.RoomContractSets.Min(r => r.Rate.FinalPrice.Amount);
                        var maxPrice = accommodationAvailability.RoomContractSets.Max(r => r.Rate.FinalPrice.Amount);

                        var roomContractSets = accommodationAvailability.RoomContractSets
                            .Select(rs => rs.ToRoomContractSet(supplier, rs.IsDirectContract))
                            .Where(roomSet => RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, connectorRequest.CheckInDate, searchSettings,
                                _dateTimeProvider))
                            .ToList();

                        htIdMapping.TryGetValue(accommodationAvailability.AccommodationId, out var htId);
                        
                        return new AccommodationAvailabilityResult(timestamp,
                            details.AvailabilityId,
                            roomContractSets,
                            minPrice,
                            maxPrice,
                            connectorRequest.CheckInDate,
                            connectorRequest.CheckOutDate,
                            htId,
                            accommodationAvailability.AccommodationId);
                    })
                    .Where(a => !a.Equals(default) && a.RoomContractSets.Any())
                    .ToList();
            }


            Task SaveResult(List<AccommodationAvailabilityResult> results) 
                => _storage.SaveResults(searchId, supplier, results);


            Task NotifyClient() 
                => _hubContext.Clients.Group(agent.AgentId.ToString()).SearchStateChanged(new SearchStateChangedMessage(searchId));


            Task SaveState(Result<List<AccommodationAvailabilityResult>, ProblemDetails> result)
            {
                var state = result.IsSuccess
                    ? SupplierAvailabilitySearchState.Completed(searchId, result.Value.Select(r => r.HtId).ToList(), result.Value.Count)
                    : SupplierAvailabilitySearchState.Failed(searchId, result.Error.Detail);

                if (state.TaskState == AvailabilitySearchTaskState.Completed)
                {
                    _logger.LogProviderAvailabilitySearchSuccess(searchId, supplier, state.ResultCount);
                }
                else
                {
                    _logger.LogProviderAvailabilitySearchFailure(searchId, supplier, state.TaskState, state.Error);
                }

                return _storage.SaveState(searchId, state, supplier);
            }
        }
        
        
        private static AvailabilityRequest CreateRequest(Models.Availabilities.AvailabilityRequest request, List<SupplierCodeMapping> mappings,
            AccommodationBookingSettings searchSettings)
        {
            var roomDetails = request.RoomDetails
                .Select(r => new RoomOccupationRequest(r.AdultsNumber, r.ChildrenAges, r.Type, r.IsExtraBedNeeded))
                .ToList();

            var searchFilters = Convert(request.Filters);

            var supplierAccommodationCodes = mappings.Select(m => m.SupplierCode).ToList();
            
            return new AvailabilityRequest(nationality: request.Nationality,
                residency: request.Residency, 
                checkInDate: request.CheckInDate,
                checkOutDate: request.CheckOutDate, 
                filters: searchFilters | searchSettings.AdditionalSearchFilters,
                rooms: roomDetails, 
                propertyTypes: request.PropertyType,
                ratings: request.Ratings,
                accommodationIds: supplierAccommodationCodes);


            static EdoContracts.General.Enums.SearchFilters Convert(ClientSearchFilters filters)
            {
                EdoContracts.General.Enums.SearchFilters resultedFilter = default;

                if (filters.HasFlag(ClientSearchFilters.AvailableOnly))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.AvailableOnly;

                if (filters.HasFlag(ClientSearchFilters.AvailableWeighted))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.AvailableWeighted;

                if (filters.HasFlag(ClientSearchFilters.BestArrangement))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.BestArrangement;

                if (filters.HasFlag(ClientSearchFilters.BestContract))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.BestContract;

                if (filters.HasFlag(ClientSearchFilters.BestPrice))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.BestPrice;

                if (filters.HasFlag(ClientSearchFilters.ExcludeDynamic))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.ExcludeDynamic;

                if (filters.HasFlag(ClientSearchFilters.BestRoomPlans))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.BestRoomPlans;

                if (filters.HasFlag(ClientSearchFilters.ExcludeNonRefundable))
                    resultedFilter |= EdoContracts.General.Enums.SearchFilters.ExcludeNonRefundable;

                return resultedFilter;
            }
        }


        private readonly IWideAvailabilityPriceProcessor _priceProcessor;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<WideAvailabilitySearchTask> _logger;
        private readonly IWideAvailabilityStorage _storage;
        private readonly IHubContext<SearchHub, ISearchHub> _hubContext;
        private readonly IAccommodationMapperClient _mapperClient;
    }
}