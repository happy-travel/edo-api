using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities.Mapping;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Mapping;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.AccommodationMappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilitySearchService : IWideAvailabilitySearchService
    {
        public WideAvailabilitySearchService(IAccommodationDuplicatesService duplicatesService,
            ILocationService locationService,
            IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IWideAvailabilityStorage availabilityStorage,
            IServiceScopeFactory serviceScopeFactory,
            AvailabilityAnalyticsService analyticsService,
            IAvailabilitySearchAreaService searchAreaService,
            ILogger<WideAvailabilitySearchService> logger)
        {
            _duplicatesService = duplicatesService;
            _locationService = locationService;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _availabilityStorage = availabilityStorage;
            _serviceScopeFactory = serviceScopeFactory;
            _analyticsService = analyticsService;
            _searchAreaService = searchAreaService;
            _logger = logger;
        }
        
   
        public async Task<Result<Guid>> StartSearch(AvailabilityRequest request, AgentContext agent, string languageCode)
        {
            var searchId = Guid.NewGuid();
            _logger.LogMultiProviderAvailabilitySearchStarted($"Starting availability search with id '{searchId}'");

            List<Location> locations;
            Dictionary<Suppliers, List<SupplierCodeMapping>> accommodationCodes = new Dictionary<Suppliers, List<SupplierCodeMapping>>();
            // Old flow
            if (request.HtIds is null || !request.HtIds.Any())
            {
                var locationResult = await _locationService.Get(request.Location, languageCode);
                if (locationResult.IsFailure)
                    return Result.Failure<Guid>(locationResult.Error.Detail);
            
                locations = new List<Location>() {locationResult.Value};
            }
            // New flow
            else
            {
                var (_, isFailure, searchArea, error) = await _searchAreaService.GetSearchArea(request.HtIds, languageCode);
                if (isFailure)
                    return Result.Failure<Guid>(error);

                locations = searchArea.Locations;
                accommodationCodes = searchArea.AccommodationCodes;
            }

            _analyticsService.LogWideAvailabilitySearch(request, searchId, locations, agent, languageCode);
            
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);

            // TODO: This is used in old flow only, remove when switching to new flow
            var location = locations.First();
            StartSearchTasks(searchId, request, searchSettings, location, accommodationCodes, agent, languageCode);
            
            return searchId;
        }


        public async Task<WideAvailabilitySearchState> GetState(Guid searchId, AgentContext agent)
        {
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            var searchStates = await _availabilityStorage.GetStates(searchId, searchSettings.EnabledConnectors);
            return WideAvailabilitySearchState.FromSupplierStates(searchId, searchStates);
        }
        
        public async Task<IEnumerable<WideAvailabilityResult>> GetResult(Guid searchId, AgentContext agent)
        {
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            var accommodationDuplicates = await _duplicatesService.Get(agent);
            var supplierSearchResults = await _availabilityStorage.GetResults(searchId, searchSettings.EnabledConnectors);
            
            return CombineAvailabilities(supplierSearchResults);

            IEnumerable<WideAvailabilityResult> CombineAvailabilities(IEnumerable<(Suppliers ProviderKey, List<AccommodationAvailabilityResult> AccommodationAvailabilities)> availabilities)
            {
                if (availabilities == null || !availabilities.Any())
                    return Enumerable.Empty<WideAvailabilityResult>();

                return availabilities
                    .SelectMany(supplierResults =>
                    {
                        var (supplierKey, supplierAvailabilities) = supplierResults;
                        return supplierAvailabilities
                            .Select(pa => (Provider: supplierKey, Availability: pa));
                    })
                    .OrderBy(r => r.Availability.Timestamp)
                    .RemoveRepeatedAccommodations()
                    .Select(r =>
                    {
                        var (supplier, availability) = r;
                        var supplierAccommodationId = new SupplierAccommodationId(supplier, availability.Accommodation.Id);
                        var hasDuplicatesForCurrentAgent = accommodationDuplicates.Contains(supplierAccommodationId);
                        
                        return new WideAvailabilityResult(availability.Id,
                            availability.Accommodation,
                            availability.RoomContractSets,
                            availability.MinPrice,
                            availability.MaxPrice,
                            hasDuplicatesForCurrentAgent,
                            availability.CheckInDate,
                            availability.CheckOutDate,
                            searchSettings.IsSupplierVisible
                                ? supplier
                                : (Suppliers?) null,
                            availability.HtId);
                    })
                    .Where(a => a.RoomContractSets.Any());
            }
        }

        private void StartSearchTasks(Guid searchId, AvailabilityRequest request, AccommodationBookingSettings searchSettings,
            Location location, Dictionary<Suppliers, List<SupplierCodeMapping>> accommodationCodes, AgentContext agent, string languageCode)
        {
            foreach (var supplier in searchSettings.EnabledConnectors)
            {
                accommodationCodes.TryGetValue(supplier, out var supplierCodeMappings);
                // Starting search tasks in a separate thread
                Task.Run(async () =>
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    
                    await WideAvailabilitySearchTask
                        .Create(scope.ServiceProvider)
                        .Start(searchId, request, location, supplierCodeMappings ?? new (), supplier, agent, languageCode, searchSettings);
                });
            }
        }
        
        
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly ILocationService _locationService;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IWideAvailabilityStorage _availabilityStorage;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly AvailabilityAnalyticsService _analyticsService;
        private readonly IAvailabilitySearchAreaService _searchAreaService;
        private readonly ILogger<WideAvailabilitySearchService> _logger;
    }
}