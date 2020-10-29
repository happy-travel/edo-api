using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.AccommodationMappings;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionService : IRoomSelectionService
    {
        public RoomSelectionService(IDataProviderManager dataProviderManager,
            IWideAvailabilityStorage wideAvailabilityStorage,
            IAccommodationDuplicatesService duplicatesService,
            IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IDateTimeProvider dateTimeProvider,
            IServiceScopeFactory serviceScopeFactory)
        {
            _dataProviderManager = dataProviderManager;
            _wideAvailabilityStorage = wideAvailabilityStorage;
            _duplicatesService = duplicatesService;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _dateTimeProvider = dateTimeProvider;
            _serviceScopeFactory = serviceScopeFactory;
        }


        public async Task<Result<AvailabilitySearchTaskState>> GetState(Guid searchId, Guid resultId, AgentContext agent)
        {
            var (_, isFailure, selectedResult, error) =  await GetSelectedResult(searchId, resultId, agent);
            if (isFailure)
                return Result.Failure<AvailabilitySearchTaskState>(error);
            
            var providerAccommodationIds = new List<ProviderAccommodationId>
            {
                new ProviderAccommodationId(selectedResult.DataProvider, selectedResult.Result.Accommodation.Id)
            };
            
            var otherProvidersAccommodations = await _duplicatesService.GetDuplicateReports(providerAccommodationIds);
            var dataProviders = otherProvidersAccommodations
                .Select(a => a.Key.DataProvider)
                .ToList();

            var results = await _wideAvailabilityStorage.GetStates(searchId, dataProviders);
            return WideAvailabilitySearchState.FromProviderStates(searchId, results).TaskState;
        }
        
        
        public async Task<Result<Accommodation, ProblemDetails>> GetAccommodation(Guid searchId, Guid resultId, AgentContext agent, string languageCode)
        {
            var (_, isFailure, selectedResult, error) = await GetSelectedResult(searchId, resultId, agent);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<Accommodation>(error);
            
            return await _dataProviderManager
                .Get(selectedResult.DataProvider)
                .GetAccommodation(selectedResult.Result.Accommodation.Id, languageCode);
        }


        public async Task<Result<List<RoomContractSetInfo>>> Get(Guid searchId, Guid resultId, AgentContext agent, string languageCode)
        {
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            
            var (_, isFailure, selectedResults, error) = await GetSelectedWideAvailabilityResults(searchId, resultId, agent);
            if (isFailure)
                return Result.Failure<List<RoomContractSetInfo>>(error);
            
            var providerTasks = selectedResults
                .Select(GetProviderAvailability)
                .ToArray();

            await Task.WhenAll(providerTasks);

            return providerTasks
                .Select(task => task.Result)
                .Where(taskResult => taskResult.IsSuccess)
                .Select(taskResult => taskResult.Value)
                .SelectMany(MapToRoomContractSets)
                .Where(SettingsFilter)
                .ToList();


            async Task<Result<ProviderData<AccommodationAvailability>, ProblemDetails>> GetProviderAvailability((Suppliers, AccommodationAvailabilityResult) wideAvailabilityResult)
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var (source, result) = wideAvailabilityResult;

                return await RoomSelectionSearchTask
                    .Create(scope.ServiceProvider)
                    .GetProviderAvailability(searchId, resultId, source, result.Accommodation.Id, result.AvailabilityId, agent, languageCode);
            }
            

            async Task<Result<List<(Suppliers Source, AccommodationAvailabilityResult Result)>>> GetSelectedWideAvailabilityResults(Guid searchId, Guid resultId, AgentContext agent)
            {
                var results = await GetWideAvailabilityResults(searchId, agent);
                
                var selectedResult = results
                    .SingleOrDefault(r => r.Result.Id == resultId);

                if (selectedResult.Equals(default))
                    return Result.Failure<List<(Suppliers, AccommodationAvailabilityResult)>>("Could not find selected availability");

                if (searchSettings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.Hide &&
                    selectedResult.Result.CheckInDate.Date <= _dateTimeProvider.UtcTomorrow())
                {
                    return Result.Failure<List<(Suppliers, AccommodationAvailabilityResult)>>("You can't book the contract within deadline without explicit approval from a Happytravel.com officer.");
                }

                // If there is no duplicate, we'll execute request to a single provider only
                if (string.IsNullOrWhiteSpace(selectedResult.Result.DuplicateReportId))
                    return new List<(Suppliers Source, AccommodationAvailabilityResult Result)> {selectedResult};

                return results
                    .Where(r => r.Result.DuplicateReportId == selectedResult.Result.DuplicateReportId)
                    .ToList();
            }

            
            IEnumerable<RoomContractSetInfo> MapToRoomContractSets(ProviderData<AccommodationAvailability> accommodationAvailability)
            {
                return accommodationAvailability.Data.RoomContractSets
                    .Select(rs =>
                    {
                        var provider = searchSettings.IsDataProviderVisible
                            ? accommodationAvailability.Source
                            : (Suppliers?) null;

                        return RoomContractSetInfo.FromRoomContractSet(rs, provider);
                    });
            }
            

            bool SettingsFilter(RoomContractSetInfo roomSet)
            {
                if (searchSettings.AprMode == AprMode.Hide && roomSet.IsAdvancedPurchaseRate)
                    return false;

                var deadlineDate = roomSet.Deadline.Date;
                if (searchSettings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.Hide
                    && deadlineDate.HasValue && deadlineDate.Value.Date <= _dateTimeProvider.UtcTomorrow())
                {
                    return false;
                }
                
                return true;
            }
        }


        private async Task<List<(Suppliers DataProvider, AccommodationAvailabilityResult Result)>> GetWideAvailabilityResults(Guid searchId, AgentContext agent)
        {
            var settings = await _accommodationBookingSettingsService.Get(agent);
            return (await _wideAvailabilityStorage.GetResults(searchId, settings.EnabledConnectors))
                .SelectMany(r => r.AccommodationAvailabilities.Select(acr => (Source: r.ProviderKey, Result: acr)))
                .ToList();
        }


        private async Task<Result<(Suppliers DataProvider, AccommodationAvailabilityResult Result)>> GetSelectedResult(Guid searchId, Guid resultId, AgentContext agent)
        {
            var result = (await GetWideAvailabilityResults(searchId, agent))
                .SingleOrDefault(r => r.Result.Id == resultId);

            return result.Equals(default)
                ? Result.Failure<(Suppliers, AccommodationAvailabilityResult)>("Could not find selected availability")
                : result;
        }

        
        private readonly IDataProviderManager _dataProviderManager;
        private readonly IWideAvailabilityStorage _wideAvailabilityStorage;
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IServiceScopeFactory _serviceScopeFactory;
    }
}