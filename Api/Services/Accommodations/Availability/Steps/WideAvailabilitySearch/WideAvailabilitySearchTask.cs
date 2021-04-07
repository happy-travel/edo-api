using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Availabilities.Mapping;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.AccommodationMappings;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AvailabilityRequest = HappyTravel.EdoContracts.Accommodations.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilitySearchTask
    {
        private WideAvailabilitySearchTask(IWideAvailabilityStorage storage,
            IWideAvailabilityPriceProcessor priceProcessor,
            IAccommodationDuplicatesService duplicatesService,
            ISupplierConnectorManager supplierConnectorManager,
            IDateTimeProvider dateTimeProvider,
            ILogger<WideAvailabilitySearchTask> logger,
            IOptions<BookingOptions> bookingOptions)
        {
            _storage = storage;
            _priceProcessor = priceProcessor;
            _duplicatesService = duplicatesService;
            _supplierConnectorManager = supplierConnectorManager;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _bookingOptions = bookingOptions.Value;
        }


        public static WideAvailabilitySearchTask Create(IServiceProvider serviceProvider)
        {
            return new(
                serviceProvider.GetRequiredService<IWideAvailabilityStorage>(),
                serviceProvider.GetRequiredService<IWideAvailabilityPriceProcessor>(),
                serviceProvider.GetRequiredService<IAccommodationDuplicatesService>(),
                serviceProvider.GetRequiredService<ISupplierConnectorManager>(),
                serviceProvider.GetRequiredService<IDateTimeProvider>(),
                serviceProvider.GetRequiredService<ILogger<WideAvailabilitySearchTask>>(),
                serviceProvider.GetRequiredService<IOptions<BookingOptions>>()
            );
        }


        public async Task Start(Guid searchId, Models.Availabilities.AvailabilityRequest availabilityRequest, 
            Location location, List<SupplierCodeMapping> accommodationCodeMappings, Suppliers supplier, 
            AgentContext agent, string languageCode,
            AccommodationBookingSettings searchSettings)
        {
            var supplierConnector = _supplierConnectorManager.Get(supplier);
            var connectorRequest = CreateRequest(availabilityRequest, location, accommodationCodeMappings, searchSettings);

            try
            {
                _logger.LogProviderAvailabilitySearchStarted($"Availability search with id '{searchId}' on supplier '{supplier}' started");

                await GetAvailability(connectorRequest, languageCode)
                    .Bind(ConvertCurrencies)
                    .Map(ProcessPolicies)
                    .Map(ApplyMarkups)
                    .Map(Convert)
                    .Tap(SaveResult)
                    .Finally(SaveState);
            }
            catch (Exception ex)
            {
                // TODO: Add sentry error notification
                _logger.LogProviderAvailabilitySearchException(ex);
                var result = ProblemDetailsBuilder.Fail<List<AccommodationAvailabilityResult>>("Server error", HttpStatusCode.InternalServerError);
                await SaveState(result);
            }


            async Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> GetAvailability(AvailabilityRequest request,
                string languageCode)
            {
                var saveToStorageTask = _storage.SaveState(searchId, SupplierAvailabilitySearchState.Pending(searchId), supplier);
                var getAvailabilityTask = supplierConnector.GetAvailability(request, languageCode);
                await Task.WhenAll(saveToStorageTask, getAvailabilityTask);

                return getAvailabilityTask.Result;
            }


            Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> ConvertCurrencies(EdoContracts.Accommodations.Availability availabilityDetails) 
                => _priceProcessor.ConvertCurrencies(availabilityDetails, agent);


            Task<EdoContracts.Accommodations.Availability> ApplyMarkups(EdoContracts.Accommodations.Availability response) 
                => _priceProcessor.ApplyMarkups(response, agent);


            EdoContracts.Accommodations.Availability ProcessPolicies(EdoContracts.Accommodations.Availability response) 
                => WideAvailabilityPolicyProcessor.Process(response, searchSettings.CancellationPolicyProcessSettings);


            async Task<List<AccommodationAvailabilityResult>> Convert(EdoContracts.Accommodations.Availability details)
            {
                var supplierAccommodationIds = details.Results
                    .Select(r => new SupplierAccommodationId(supplier, r.Accommodation.Id))
                    .Distinct()
                    .ToList();

                var duplicates = await _duplicatesService.GetDuplicateReports(supplierAccommodationIds);

                var htIdMapping = accommodationCodeMappings.ToDictionary(m => m.SupplierCode, m => m.HtId);

                var timestamp = _dateTimeProvider.UtcNow().Ticks;
                return details
                    .Results
                    .Select(accommodationAvailability =>
                    {
                        var minPrice = accommodationAvailability.RoomContractSets.Min(r => r.Rate.FinalPrice.Amount);
                        var maxPrice = accommodationAvailability.RoomContractSets.Max(r => r.Rate.FinalPrice.Amount);
                        var accommodationId = new SupplierAccommodationId(supplier, accommodationAvailability.Accommodation.Id);
                        var resultId = Guid.NewGuid();
                        var duplicateReportId = duplicates.TryGetValue(accommodationId, out var reportId)
                            ? reportId
                            : string.Empty;

                        var roomContractSets = accommodationAvailability.RoomContractSets
                            .Select(rs => rs.ToRoomContractSet(supplier, rs.IsDirectContract, _bookingOptions.CreditCardPaymentsCommission))
                            .Where(roomSet => RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, connectorRequest.CheckInDate, searchSettings,
                                _dateTimeProvider))
                            .ToList();

                        htIdMapping.TryGetValue(accommodationAvailability.Accommodation.Id, out var htId);

                        return new AccommodationAvailabilityResult(resultId,
                            timestamp,
                            details.AvailabilityId,
                            accommodationAvailability.Accommodation,
                            roomContractSets,
                            duplicateReportId,
                            minPrice,
                            maxPrice,
                            connectorRequest.CheckInDate,
                            connectorRequest.CheckOutDate,
                            htId);
                    })
                    .Where(a => a.RoomContractSets.Any())
                    .ToList();
            }


            Task SaveResult(List<AccommodationAvailabilityResult> results) 
                => _storage.SaveResults(searchId, supplier, results);


            Task SaveState(Result<List<AccommodationAvailabilityResult>, ProblemDetails> result)
            {
                var state = result.IsSuccess
                    ? SupplierAvailabilitySearchState.Completed(searchId, result.Value.Select(r => r.DuplicateReportId).ToList(), result.Value.Count)
                    : SupplierAvailabilitySearchState.Failed(searchId, result.Error.Detail);

                if (state.TaskState == AvailabilitySearchTaskState.Completed)
                {
                    _logger.LogProviderAvailabilitySearchSuccess(
                        $"Availability search with id '{searchId}' on supplier '{supplier}' finished successfully with '{state.ResultCount}' results");
                }
                else
                {
                    _logger.LogProviderAvailabilitySearchFailure(
                        $"Availability search with id '{searchId}' on supplier '{supplier}' finished with state '{state.TaskState}', error '{state.Error}'");
                }

                return _storage.SaveState(searchId, state, supplier);
            }
        }
        
        
        private static AvailabilityRequest CreateRequest(Models.Availabilities.AvailabilityRequest request, Location location, List<SupplierCodeMapping> mappings,
            AccommodationBookingSettings searchSettings)
        {
            var roomDetails = request.RoomDetails
                .Select(r => new RoomOccupationRequest(r.AdultsNumber, r.ChildrenAges, r.Type, r.IsExtraBedNeeded))
                .ToList();

            var searchFilters = Convert(request.Filters);

            if (request.HtIds is null || !request.HtIds.Any())
            {
                // Same request for all suppliers
                // Remove when will support the new flow only
                return new AvailabilityRequest(request.Nationality, request.Residency, request.CheckInDate,
                    request.CheckOutDate,
                    searchFilters | searchSettings.AdditionalSearchFilters,
                    roomDetails,
                    new EdoContracts.GeoData.Location(location.Name, location.Locality, location.Country, 
                        location.Coordinates, location.Distance,
                        location.Source, location.Type),
                    request.PropertyType, request.Ratings, new List<string>(0));
            }
            else
            {
                var supplierAccommodationCodes = mappings.Select(m => m.SupplierCode).ToList();
                return new AvailabilityRequest(request.Nationality,
                    request.Residency,
                    request.CheckInDate,
                    request.CheckOutDate,
                    searchFilters | searchSettings.AdditionalSearchFilters,
                    roomDetails,
                    null,
                    request.PropertyType, request.Ratings, supplierAccommodationCodes);
            }


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
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<WideAvailabilitySearchTask> _logger;
        private readonly IWideAvailabilityStorage _storage;
        private readonly BookingOptions _bookingOptions;
    }
}