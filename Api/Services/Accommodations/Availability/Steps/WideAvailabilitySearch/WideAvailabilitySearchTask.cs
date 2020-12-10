using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.AccommodationMappings;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AvailabilityRequest = HappyTravel.EdoContracts.Accommodations.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch
{
    public class WideAvailabilitySearchTask
    {
        private WideAvailabilitySearchTask(IWideAvailabilityStorage storage,
            IPriceProcessor priceProcessor,
            IAccommodationDuplicatesService duplicatesService,
            ISupplierConnectorManager supplierConnectorManager,
            IDateTimeProvider dateTimeProvider,
            ILogger<WideAvailabilitySearchTask> logger)
        {
            _storage = storage;
            _priceProcessor = priceProcessor;
            _duplicatesService = duplicatesService;
            _supplierConnectorManager = supplierConnectorManager;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }


        public static WideAvailabilitySearchTask Create(IServiceProvider serviceProvider)
        {
            return new WideAvailabilitySearchTask(
                serviceProvider.GetRequiredService<IWideAvailabilityStorage>(),
                serviceProvider.GetRequiredService<IPriceProcessor>(),
                serviceProvider.GetRequiredService<IAccommodationDuplicatesService>(),
                serviceProvider.GetRequiredService<ISupplierConnectorManager>(),
                serviceProvider.GetRequiredService<IDateTimeProvider>(),
                serviceProvider.GetRequiredService<ILogger<WideAvailabilitySearchTask>>()
            );
        }


        public async Task Start(Guid searchId, AvailabilityRequest request, Suppliers supplier, AgentContext agent, string languageCode,
            AccommodationBookingSettings searchSettings)
        {
            var supplierConnector = _supplierConnectorManager.Get(supplier);

            try
            {
                _logger.LogProviderAvailabilitySearchStarted($"Availability search with id '{searchId}' on supplier '{supplier}' started");

                await GetAvailability(request, languageCode)
                    .Bind(ConvertCurrencies)
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


            async Task<Result<EdoContracts.Accommodations.Availability, ProblemDetails>> ConvertCurrencies(EdoContracts.Accommodations.Availability availabilityDetails)
            {
                var convertedResults = new List<SlimAccommodationAvailability>(availabilityDetails.Results.Count);
                foreach (var slimAccommodationAvailability in availabilityDetails.Results)
                {
                    // Currency can differ in different results
                    var (_, isFailure, convertedAccommodationAvailability, error) = await _priceProcessor.ConvertCurrencies(agent, slimAccommodationAvailability, AvailabilityResultsExtensions.ProcessPrices,
                        AvailabilityResultsExtensions.GetCurrency);

                    if (isFailure)
                        return Result.Failure<EdoContracts.Accommodations.Availability, ProblemDetails>(error);
                    
                    convertedResults.Add(convertedAccommodationAvailability);
                }

                return new EdoContracts.Accommodations.Availability(availabilityDetails.AvailabilityId, availabilityDetails.NumberOfNights,
                    availabilityDetails.CheckInDate, availabilityDetails.CheckOutDate, convertedResults, availabilityDetails.NumberOfProcessedAccommodations);
            }


            Task<EdoContracts.Accommodations.Availability> ApplyMarkups(EdoContracts.Accommodations.Availability response) 
                => _priceProcessor.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);


            async Task<List<AccommodationAvailabilityResult>> Convert(EdoContracts.Accommodations.Availability details)
            {
                var supplierAccommodationIds = details.Results
                    .Select(r => new SupplierAccommodationId(supplier, r.Accommodation.Id))
                    .Distinct()
                    .ToList();

                var duplicates = await _duplicatesService.GetDuplicateReports(supplierAccommodationIds);

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
                            .ToRoomContractSetList()
                            .ApplySearchFilters(searchSettings, _dateTimeProvider, request.CheckInDate);

                        return new AccommodationAvailabilityResult(resultId,
                            timestamp,
                            details.AvailabilityId,
                            accommodationAvailability.Accommodation,
                            roomContractSets,
                            duplicateReportId,
                            minPrice,
                            maxPrice,
                            request.CheckInDate,
                            request.CheckOutDate);
                    })
                    .Where(a => a.RoomContractSets.Any())
                    .ToList();
            }


            Task SaveResult(List<AccommodationAvailabilityResult> results) => _storage.SaveResults(searchId, supplier, results);


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


        private readonly IPriceProcessor _priceProcessor;
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<WideAvailabilitySearchTask> _logger;
        private readonly IWideAvailabilityStorage _storage;
    }
}