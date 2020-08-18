using System;
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
using HappyTravel.EdoContracts.Accommodations;
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
            IDataProviderFactory dataProviderFactory,
            IDateTimeProvider dateTimeProvider,
            ILogger<WideAvailabilitySearchTask> logger)
        {
            _storage = storage;
            _priceProcessor = priceProcessor;
            _duplicatesService = duplicatesService;
            _dataProviderFactory = dataProviderFactory;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }


        public static WideAvailabilitySearchTask Create(IServiceProvider serviceProvider)
        {
            return new WideAvailabilitySearchTask(
                serviceProvider.GetRequiredService<IWideAvailabilityStorage>(),
                serviceProvider.GetRequiredService<IPriceProcessor>(),
                serviceProvider.GetRequiredService<IAccommodationDuplicatesService>(),
                serviceProvider.GetRequiredService<IDataProviderFactory>(),
                serviceProvider.GetRequiredService<IDateTimeProvider>(),
                serviceProvider.GetRequiredService<ILogger<WideAvailabilitySearchTask>>()
            );
        }


        public async Task Start(Guid searchId, AvailabilityRequest request, DataProviders provider, AgentContext agent, string languageCode)
        {
            var dataProvider = _dataProviderFactory.Get(provider);
            try
            {
                _logger.LogProviderAvailabilitySearchStarted($"Availability search with id '{searchId}' on provider '{provider}' started");

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
                var result = ProblemDetailsBuilder.Fail<AccommodationAvailabilityResult[]>("Server error", HttpStatusCode.InternalServerError);
                await SaveState(result);
            }


            async Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest request,
                string languageCode)
            {
                var saveToStorageTask = _storage.SaveState(searchId, ProviderAvailabilitySearchState.Pending(searchId), provider);
                var getAvailabilityTask = dataProvider.GetAvailability(request, languageCode);
                await Task.WhenAll(saveToStorageTask, getAvailabilityTask);

                return getAvailabilityTask.Result;
            }


            Task<Result<AvailabilityDetails, ProblemDetails>> ConvertCurrencies(AvailabilityDetails availabilityDetails)
                => _priceProcessor.ConvertCurrencies(agent, availabilityDetails, AvailabilityResultsExtensions.ProcessPrices,
                    AvailabilityResultsExtensions.GetCurrency);


            async Task<AvailabilityDetails> ApplyMarkups(AvailabilityDetails response)
            {
                var markup = await _priceProcessor.ApplyMarkups(agent, response, AvailabilityResultsExtensions.ProcessPrices);
                return markup.Data;
            }


            async Task<AccommodationAvailabilityResult[]> Convert(AvailabilityDetails details)
            {
                var providerAccommodationIds = details.Results
                    .Select(r => new ProviderAccommodationId(provider, r.AccommodationDetails.Id))
                    .ToList();

                var duplicates = await _duplicatesService.GetDuplicateReports(providerAccommodationIds);

                var timestamp = _dateTimeProvider.UtcNow().Ticks;
                return details
                    .Results
                    .Select(accommodationAvailability =>
                    {
                        var minPrice = accommodationAvailability.RoomContractSets.Min(r => r.Price.NetTotal);
                        var maxPrice = accommodationAvailability.RoomContractSets.Max(r => r.Price.NetTotal);
                        var accommodationId = new ProviderAccommodationId(provider, accommodationAvailability.AccommodationDetails.Id);
                        var resultId = Guid.NewGuid();
                        var duplicateReportId = duplicates.TryGetValue(accommodationId, out var reportId)
                            ? reportId
                            : string.Empty;

                        return new AccommodationAvailabilityResult(resultId,
                            timestamp,
                            details.AvailabilityId,
                            accommodationAvailability.AccommodationDetails,
                            accommodationAvailability.RoomContractSets,
                            duplicateReportId,
                            minPrice,
                            maxPrice);
                    })
                    .ToArray();
            }


            Task SaveResult(AccommodationAvailabilityResult[] results) => _storage.SaveResults(searchId, provider, results);


            Task SaveState(Result<AccommodationAvailabilityResult[], ProblemDetails> result)
            {
                var state = result.IsSuccess
                    ? ProviderAvailabilitySearchState.Completed(searchId, result.Value.Length)
                    : ProviderAvailabilitySearchState.Failed(searchId, result.Error.Detail);

                if (state.TaskState == AvailabilitySearchTaskState.Completed)
                {
                    _logger.LogProviderAvailabilitySearchSuccess(
                        $"Availability search with id '{searchId}' on provider '{provider}' finished successfully with '{state.ResultCount}' results");
                }
                else
                {
                    _logger.LogProviderAvailabilitySearchFailure(
                        $"Availability search with id '{searchId}' on provider '{provider}' finished with state '{state.TaskState}', error '{state.Error}'");
                }

                return _storage.SaveState(searchId, state, provider);
            }
        }


        private readonly IPriceProcessor _priceProcessor;
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<WideAvailabilitySearchTask> _logger;
        private readonly IWideAvailabilityStorage _storage;
    }
}