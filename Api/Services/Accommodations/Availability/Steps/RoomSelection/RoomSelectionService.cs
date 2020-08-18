using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch;
using HappyTravel.Edo.Api.Services.Accommodations.Mappings;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.AccommodationMappings;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionService : IRoomSelectionService
    {
        public RoomSelectionService(IDataProviderFactory dataProviderFactory,
            IWideAvailabilityStorage wideAvailabilityStorage,
            IOptions<DataProviderOptions> providerOptions,
            IAccommodationDuplicatesService duplicatesService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _dataProviderFactory = dataProviderFactory;
            _wideAvailabilityStorage = wideAvailabilityStorage;
            _duplicatesService = duplicatesService;
            _serviceScopeFactory = serviceScopeFactory;
            _providerOptions = providerOptions.Value;
        }


        public async Task<AvailabilitySearchTaskState> GetState(Guid searchId, Guid resultId)
        {
            var selectedResult =  await GetSelectedResult(searchId, resultId);
            var providerAccommodationIds = new List<ProviderAccommodationId>
            {
                new ProviderAccommodationId(selectedResult.DataProvider, selectedResult.Result.AccommodationDetails.Id)
            };
            
            var otherProvidersAccommodations = await _duplicatesService.GetDuplicateReports(providerAccommodationIds);
            var dataProviders = otherProvidersAccommodations
                .Select(a => a.Key.DataProvider)
                .ToList();

            var results = await _wideAvailabilityStorage.GetStates(searchId, dataProviders);
            return WideAvailabilitySearchState.FromProviderStates(searchId, results).TaskState;
        }
        
        
        public async Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(Guid searchId, Guid resultId, string languageCode)
        {
            var selectedResult = await GetSelectedResult(searchId, resultId);
            return await _dataProviderFactory.Get(selectedResult.DataProvider).GetAccommodation(selectedResult.Result.AccommodationDetails.Id, languageCode);
        }


        public async Task<Result<List<RoomContractSet>>> Get(Guid searchId, Guid resultId, AgentContext agent, string languageCode)
        {
            var providerTasks = (await GetSelectedWideAvailabilityResults(searchId, resultId))
                .Select(GetProviderAvailability)
                .ToArray();

            await Task.WhenAll(providerTasks);

            return providerTasks
                .Select(task => task.Result)
                .Where(taskResult => taskResult.IsSuccess)
                .Select(taskResult => taskResult.Value)
                .SelectMany(accommodationAvailability => accommodationAvailability.Data.RoomContractSets)
                .ToList();


            async Task<Result<ProviderData<SingleAccommodationAvailabilityDetails>, ProblemDetails>> GetProviderAvailability((DataProviders, AccommodationAvailabilityResult) wideAvailabilityResult)
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var (source, result) = wideAvailabilityResult;

                return await RoomSelectionSearchTask.Create(scope.ServiceProvider)
                    .GetProviderAvailability(searchId, resultId, source, result.AccommodationDetails.Id, result.AvailabilityId, agent, languageCode);
            }
            

            async Task<List<(DataProviders Source, AccommodationAvailabilityResult Result)>> GetSelectedWideAvailabilityResults(Guid searchId, Guid resultId)
            {
                var results = await GetWideAvailabilityResults(searchId);
                
                var selectedResult = results
                    .Single(r => r.Result.Id == resultId);

                // If there is no duplicate, we'll execute request to a single provider only
                if (string.IsNullOrWhiteSpace(selectedResult.Result.DuplicateReportId))
                    return new List<(DataProviders Source, AccommodationAvailabilityResult Result)> {selectedResult};

                return results
                    .Where(r => r.Result.DuplicateReportId == selectedResult.Result.DuplicateReportId)
                    .ToList();
            }
        }


        private async Task<(DataProviders DataProvider, AccommodationAvailabilityResult Result)[]> GetWideAvailabilityResults(Guid searchId)
        {
            return (await _wideAvailabilityStorage.GetResults(searchId, _providerOptions.EnabledProviders))
                .SelectMany(r => r.AccommodationAvailabilities.Select(acr => (Source: r.ProviderKey, Result: acr)))
                .ToArray();
        }


        private async Task<(DataProviders DataProvider, AccommodationAvailabilityResult Result)> GetSelectedResult(Guid searchId, Guid resultId)
        {
            return (await GetWideAvailabilityResults(searchId))
                .Single(r => r.Result.Id == resultId);
        }

        
        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly IWideAvailabilityStorage _wideAvailabilityStorage;
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly DataProviderOptions _providerOptions;
    }
}