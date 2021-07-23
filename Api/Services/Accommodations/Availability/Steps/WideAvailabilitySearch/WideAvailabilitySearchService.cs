using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities.Mapping;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Mapping;
using HappyTravel.SuppliersCatalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilitySearchService : IWideAvailabilitySearchService
    {
        public WideAvailabilitySearchService(IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IWideAvailabilityStorage availabilityStorage, IServiceScopeFactory serviceScopeFactory, AvailabilityAnalyticsService analyticsService,
            IAvailabilitySearchAreaService searchAreaService, IDateTimeProvider dateTimeProvider, IWideAvailabilityAccommodationsStorage accommodationsStorage,
            ILogger<WideAvailabilitySearchService> logger)
        {
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _availabilityStorage = availabilityStorage;
            _serviceScopeFactory = serviceScopeFactory;
            _analyticsService = analyticsService;
            _searchAreaService = searchAreaService;
            _dateTimeProvider = dateTimeProvider;
            _accommodationsStorage = accommodationsStorage;
            _logger = logger;
        }
        
   
        public async Task<Result<Guid>> StartSearch(AvailabilityRequest request, AgentContext agent, string languageCode)
        {
            if (!request.HtIds.Any())
                return Result.Failure<Guid>($"{nameof(request.HtIds)} must not be empty");
            
            if (request.CheckInDate.Date < _dateTimeProvider.UtcToday())
                return Result.Failure<Guid>("Check in date must not be in the past");
            
            var searchId = Guid.NewGuid();
            
            Baggage.SetSearchId(searchId);
            _logger.LogMultiProviderAvailabilitySearchStarted(searchId);

            var (_, isFailure, searchArea, error) = await _searchAreaService.GetSearchArea(request.HtIds, languageCode);
            if (isFailure)
                return Result.Failure<Guid>(error);

            _analyticsService.LogWideAvailabilitySearch(request, searchId, searchArea.Locations, agent, languageCode);
            
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            await StartSearch(searchId, request, searchSettings, searchArea.AccommodationCodes, agent, languageCode);
                
            return searchId;
        }


        public async Task<WideAvailabilitySearchState> GetState(Guid searchId, AgentContext agent)
        {
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            var searchStates = await _availabilityStorage.GetStates(searchId, searchSettings.EnabledConnectors);
            return WideAvailabilitySearchState.FromSupplierStates(searchId, searchStates);
        }

        
        public async Task<IEnumerable<WideAvailabilityResult>> GetResult(Guid searchId, int top, int skip, AgentContext agent, string languageCode)
        {
            Baggage.SetSearchId(searchId);
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            var supplierSearchResults = await _availabilityStorage.GetResults(searchId, top, skip, searchSettings.EnabledConnectors);
            var htIds = supplierSearchResults
                .Select(r => r.AccommodationAvailabilities.HtId)
                .ToList();

            await _accommodationsStorage.EnsureAccommodationsCached(htIds, languageCode);

            return supplierSearchResults.Select(r =>
            {
                var accommodation = _accommodationsStorage.GetAccommodation(r.AccommodationAvailabilities.HtId, languageCode);

                var roomContractSets = r.AccommodationAvailabilities.RoomContractSets
                    .Select(rs => rs.ApplySearchSettings(isSupplierVisible: searchSettings.IsSupplierVisible,
                        isDirectContractsVisible: searchSettings.IsDirectContractFlagVisible))
                    .ToList();

                return new WideAvailabilityResult(accommodation,
                    roomContractSets,
                    r.AccommodationAvailabilities.MinPrice,
                    r.AccommodationAvailabilities.MaxPrice,
                    r.AccommodationAvailabilities.CheckInDate,
                    r.AccommodationAvailabilities.CheckOutDate,
                    searchSettings.IsSupplierVisible
                        ? r.SupplierKey
                        : null,
                    r.AccommodationAvailabilities.HtId);
            });
        }


        private async Task StartSearch(Guid searchId, AvailabilityRequest request, AccommodationBookingSettings searchSettings, Dictionary<Suppliers, List<SupplierCodeMapping>> accommodationCodes, AgentContext agent, string languageCode)
        {
            foreach (var supplier in searchSettings.EnabledConnectors)
            {
                if (!accommodationCodes.TryGetValue(supplier, out var supplierCodeMappings))
                {
                    await _availabilityStorage.SaveState(searchId, SupplierAvailabilitySearchState.Completed(searchId, new List<string>(0), 0), supplier);
                    continue;
                }
                
                // Starting search tasks in a separate thread
                StartSearchTask(supplier, supplierCodeMappings);
            }


            void StartSearchTask(Suppliers supplier, List<SupplierCodeMapping> supplierCodeMappings)
            {
                Task.Run(async () =>
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    
                    await WideAvailabilitySearchTask
                        .Create(scope.ServiceProvider)
                        .Start(searchId, request, supplierCodeMappings, supplier, agent, languageCode, searchSettings);
                });
            }
        }
        
        
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IWideAvailabilityStorage _availabilityStorage;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly AvailabilityAnalyticsService _analyticsService;
        private readonly IAvailabilitySearchAreaService _searchAreaService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWideAvailabilityAccommodationsStorage _accommodationsStorage;
        private readonly ILogger<WideAvailabilitySearchService> _logger;
    }
}