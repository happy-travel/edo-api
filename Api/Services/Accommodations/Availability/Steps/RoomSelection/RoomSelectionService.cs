using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using IDateTimeProvider = HappyTravel.Edo.Api.Infrastructure.IDateTimeProvider;
using RoomContractSet = HappyTravel.Edo.Api.Models.Accommodations.RoomContractSet;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionService : IRoomSelectionService
    {
        public RoomSelectionService(ISupplierConnectorManager supplierConnectorManager,
            IWideAvailabilityStorage wideAvailabilityStorage,
            IAccommodationDuplicatesService duplicatesService,
            IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IDateTimeProvider dateTimeProvider,
            IServiceScopeFactory serviceScopeFactory,
            AvailabilityAnalyticsService analyticsService,
            IOptions<BookingOptions> bookingOptions)
        {
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _dateTimeProvider = dateTimeProvider;
            _duplicatesService = duplicatesService;
            _serviceScopeFactory = serviceScopeFactory;
            _analyticsService = analyticsService;
            _supplierConnectorManager = supplierConnectorManager;
            _wideAvailabilityStorage = wideAvailabilityStorage;
            _bookingOptions = bookingOptions.Value;
        }


        public async Task<Result<AvailabilitySearchTaskState>> GetState(Guid searchId, Guid resultId, AgentContext agent)
        {
            var (_, isFailure, selectedResult, error) = await GetSelectedResult(searchId, resultId, agent);
            if (isFailure)
                return Result.Failure<AvailabilitySearchTaskState>(error);
            
            var supplierAccommodationIds = new List<SupplierAccommodationId>
            {
                new (selectedResult.Supplier, selectedResult.Result.Accommodation.Id)
            };
            
            var otherSuppliersAccommodations = await _duplicatesService.GetDuplicateReports(supplierAccommodationIds);
            var suppliers = otherSuppliersAccommodations
                .Select(a => a.Key.Supplier)
                .ToList();

            var results = await _wideAvailabilityStorage.GetStates(searchId, suppliers);
            return WideAvailabilitySearchState.FromSupplierStates(searchId, results).TaskState;
        }
        
        
        public async Task<Result<Accommodation, ProblemDetails>> GetAccommodation(Guid searchId, Guid resultId, AgentContext agent, string languageCode)
        {
            var (_, isFailure, selectedResult, error) = await GetSelectedResult(searchId, resultId, agent);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<Accommodation>(error);

            _analyticsService.LogAccommodationAvailabilityRequested(selectedResult.Result, searchId, resultId, agent);
            
            return await _supplierConnectorManager
                .Get(selectedResult.Supplier)
                .GetAccommodation(selectedResult.Result.Accommodation.Id, languageCode);
        }


        public async Task<Result<List<RoomContractSet>>> Get(Guid searchId, Guid resultId, AgentContext agent, string languageCode)
        {
            var searchSettings = await _accommodationBookingSettingsService.Get(agent);
            
            var (_, isFailure, selectedResults, error) = await GetSelectedWideAvailabilityResults(searchId, resultId, agent);
            if (isFailure)
                return Result.Failure<List<RoomContractSet>>(error);

            var checkInDate = selectedResults
                .Select(s => s.Result.CheckInDate)
                .FirstOrDefault();

            var supplierTasks = selectedResults
                .Select(GetSupplierAvailability)
                .ToArray();

            await Task.WhenAll(supplierTasks);

            return supplierTasks
                .Select(task => task.Result)
                .Where(taskResult => taskResult.IsSuccess)
                .Select(taskResult => taskResult.Value)
                .SelectMany(MapToRoomContractSets)
                .Where(SettingsFilter)
                .ToList();


            async Task<Result<SupplierData<AccommodationAvailability>, ProblemDetails>> GetSupplierAvailability((Suppliers, AccommodationAvailabilityResult) wideAvailabilityResult)
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var (source, result) = wideAvailabilityResult;

                return await RoomSelectionSearchTask
                    .Create(scope.ServiceProvider)
                    .GetSupplierAvailability(searchId, resultId, source, result.Accommodation.Id, result.AvailabilityId, searchSettings, result.HtId, agent, languageCode);
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

                // If there is no duplicate, we'll execute request to a single supplier only
                if (string.IsNullOrWhiteSpace(selectedResult.Result.DuplicateReportId))
                    return new List<(Suppliers Source, AccommodationAvailabilityResult Result)> {selectedResult};

                return results
                    .Where(r => r.Result.DuplicateReportId == selectedResult.Result.DuplicateReportId)
                    .ToList();
            }

            
            IEnumerable<RoomContractSet> MapToRoomContractSets(SupplierData<AccommodationAvailability> accommodationAvailability)
            {
                return accommodationAvailability.Data.RoomContractSets
                    .Select(rs =>
                    {
                        var supplier = searchSettings.IsSupplierVisible
                            ? accommodationAvailability.Source
                            : (Suppliers?) null;

                        var isDirectContractFlag = searchSettings.IsDirectContractFlagVisible && rs.IsDirectContract;

                        return rs.ToRoomContractSet(supplier, isDirectContractFlag, _bookingOptions.CreditCardPaymentCommission);
                    });
            }


            bool SettingsFilter(RoomContractSet roomSet) 
                => RoomContractSetSettingsChecker.IsDisplayAllowed(roomSet, checkInDate, searchSettings, _dateTimeProvider);
        }


        private async Task<List<(Suppliers Supplier, AccommodationAvailabilityResult Result)>> GetWideAvailabilityResults(Guid searchId, AgentContext agent)
        {
            var settings = await _accommodationBookingSettingsService.Get(agent);
            return (await _wideAvailabilityStorage.GetResults(searchId, settings.EnabledConnectors))
                .SelectMany(r => r.AccommodationAvailabilities.Select(acr => (Source: r.SupplierKey, Result: acr)))
                .ToList();
        }


        private async Task<Result<(Suppliers Supplier, AccommodationAvailabilityResult Result)>> GetSelectedResult(Guid searchId, Guid resultId, AgentContext agent)
        {
            var result = (await GetWideAvailabilityResults(searchId, agent))
                .SingleOrDefault(r => r.Result.Id == resultId);

            return result.Equals(default)
                ? Result.Failure<(Suppliers, AccommodationAvailabilityResult)>("Could not find selected availability")
                : result;
        }
        
        
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAccommodationDuplicatesService _duplicatesService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly AvailabilityAnalyticsService _analyticsService;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IWideAvailabilityStorage _wideAvailabilityStorage;
        private readonly BookingOptions _bookingOptions;
    }
}