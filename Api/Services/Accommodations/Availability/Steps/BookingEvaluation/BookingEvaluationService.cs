using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.SuppliersCatalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RoomContractSet = HappyTravel.EdoContracts.Accommodations.Internals.RoomContractSet;
using RoomContractSetAvailability = HappyTravel.Edo.Api.Models.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingEvaluationService : IBookingEvaluationService
    {
        public BookingEvaluationService(ISupplierConnectorManager supplierConnectorManager,
            IBookingEvaluationPriceProcessor priceProcessor,
            IRoomSelectionStorage roomSelectionStorage,
            IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IDateTimeProvider dateTimeProvider,
            IBookingEvaluationStorage bookingEvaluationStorage,
            ICounterpartyService counterpartyService,
            IAccommodationService accommodationService,
            ILogger<BookingEvaluationService> logger)
        {
            _supplierConnectorManager = supplierConnectorManager;
            _priceProcessor = priceProcessor;
            _roomSelectionStorage = roomSelectionStorage;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _dateTimeProvider = dateTimeProvider;
            _bookingEvaluationStorage = bookingEvaluationStorage;
            _counterpartyService = counterpartyService;
            _accommodationService = accommodationService;
            _logger = logger;
        }
        
        
        public async Task<Result<RoomContractSetAvailability?, ProblemDetails>> GetExactAvailability(
            Guid searchId, Guid resultId, Guid roomContractSetId, AgentContext agent, string languageCode)
        {
            Baggage.SetSearchId(searchId);
            var settings = await _accommodationBookingSettingsService.Get(agent);
            var (_, isFailure, result, error) = await GetSelectedRoomSet(searchId, resultId, roomContractSetId);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<RoomContractSetAvailability?>(error);

            var connectorEvaluationResult = await EvaluateOnConnector(result);
            if (connectorEvaluationResult.IsFailure)
            {
                _logger.LogBookingEvaluationFailure(connectorEvaluationResult.Error.Status, connectorEvaluationResult.Error.Detail);
                return (RoomContractSetAvailability?)null;
            }

            var originalSupplierPrice = connectorEvaluationResult.Value?.RoomContractSet.Rate.FinalPrice ?? default;
            
            var (_, isContractFailure, contractKind, contractError) = await _counterpartyService.GetContractKind(agent.CounterpartyId);
            if (isContractFailure)
                return ProblemDetailsBuilder.Fail<RoomContractSetAvailability?>(contractError);

            var accommodationResult = await _accommodationService.Get(result.htId, languageCode);
            if (accommodationResult.IsFailure)
            {
                _logger.LogGetAccommodationByHtIdFailed(result.htId, accommodationResult.Error.Detail);
                return (RoomContractSetAvailability?)null;
            }

            var slimAccommodation = GetSlimAccommodation(accommodationResult.Value);

            return await ConvertCurrencies(connectorEvaluationResult.Value)
                .Map(ProcessPolicies)
                .Map(ApplyMarkups)
                .Tap(SaveToCache)
                .Map(ToDetails)
                .Check(CheckAgainstSettings);


            async Task<Result<(Suppliers Supplier, RoomContractSet RoomContractSet, string AvailabilityId, string htId)>> GetSelectedRoomSet(Guid searchId, Guid resultId, Guid roomContractSetId)
            {
                var result = (await _roomSelectionStorage.GetResult(searchId, resultId, settings.EnabledConnectors))
                    .SelectMany(r =>
                    {
                        return r.Result.RoomContractSets
                            .Select(rs => (Source: r.Supplier, RoomContractSet: rs, r.Result.AvailabilityId, r.Result.HtId));
                    })
                    .SingleOrDefault(r => r.RoomContractSet.Id == roomContractSetId);

                if (result.Equals(default))
                    return Result.Failure<(Suppliers, RoomContractSet, string, string)>("Could not find selected room contract set");
                
                return result;
            }

            
            Task<Result<EdoContracts.Accommodations.RoomContractSetAvailability?, ProblemDetails>> EvaluateOnConnector((Suppliers, RoomContractSet, string, string) selectedSet)
            {
                var (supplier, roomContractSet, availabilityId, _) = selectedSet;
                return _supplierConnectorManager
                    .Get(supplier)
                    .GetExactAvailability(availabilityId, roomContractSet.Id, languageCode);
            }


            Task<Result<EdoContracts.Accommodations.RoomContractSetAvailability?, ProblemDetails>> ConvertCurrencies(EdoContracts.Accommodations.RoomContractSetAvailability? availabilityDetails) 
                => _priceProcessor.ConvertCurrencies(availabilityDetails, agent);

            
            EdoContracts.Accommodations.RoomContractSetAvailability? ProcessPolicies(EdoContracts.Accommodations.RoomContractSetAvailability? availabilityDetails) 
                => BookingEvaluationPolicyProcessor.Process(availabilityDetails, settings.CancellationPolicyProcessSettings);
            

            async Task<DataWithMarkup<EdoContracts.Accommodations.RoomContractSetAvailability?>>
                ApplyMarkups(EdoContracts.Accommodations.RoomContractSetAvailability? response)
            {
                var appliedMarkups = new List<AppliedMarkup>();
                var convertedSupplierPrice = response?.RoomContractSet.Rate.FinalPrice ?? default;
                // Saving all the changes in price that was done by markups
                Action<MarkupApplicationResult<EdoContracts.Accommodations.RoomContractSetAvailability?>> logAction = appliedMarkup =>
                {
                    if (appliedMarkup.Before is null || appliedMarkup.After is null)
                        return;
                    
                    var markupAmount = appliedMarkup.After.Value.RoomContractSet.Rate.FinalPrice - appliedMarkup.Before.Value.RoomContractSet.Rate.FinalPrice;
                    var policy = appliedMarkup.Policy;
                    appliedMarkups.Add(new AppliedMarkup(
                        scope: new MarkupPolicyScope(policy.ScopeType, policy.CounterpartyId, policy.AgencyId, policy.AgentId),
                        policyId: policy.Id,
                        amountChange: markupAmount
                    ));
                };
                
                var responseWithMarkups = await _priceProcessor.ApplyMarkups(response, agent, logAction);
                return DataWithMarkup.Create(responseWithMarkups, appliedMarkups, convertedSupplierPrice, originalSupplierPrice);
            }

            
            Task SaveToCache(DataWithMarkup<EdoContracts.Accommodations.RoomContractSetAvailability?> responseWithDeadline)
            {
                if (responseWithDeadline.Data is null)
                    return Task.CompletedTask;

                // TODO: Check that this id will not change on all connectors NIJO-823
                var finalRoomContractSet = responseWithDeadline.Data.Value.RoomContractSet;

                var paymentTypes = GetAvailablePaymentTypes(responseWithDeadline.Data.Value, contractKind);

                var dataWithMarkup = DataWithMarkup.Create(responseWithDeadline.Data.Value,
                    responseWithDeadline.AppliedMarkups, responseWithDeadline.ConvertedSupplierPrice, responseWithDeadline.OriginalSupplierPrice);

                var deadline = DeadlineMerger.CalculateMergedDeadline(finalRoomContractSet.RoomContracts);
                
                return _bookingEvaluationStorage.Set(searchId: searchId,
                    resultId: resultId,
                    roomContractSetId: finalRoomContractSet.Id, 
                    availability: dataWithMarkup, 
                    resultSupplier: result.Supplier,
                    availablePaymentTypes: paymentTypes, 
                    htId: result.htId,
                    accommodation: slimAccommodation,
                    supplierDeadline: deadline);
            }


            RoomContractSetAvailability? ToDetails(DataWithMarkup<EdoContracts.Accommodations.RoomContractSetAvailability?> availabilityDetails)
            {
                var availabilityData = availabilityDetails.Data;
                if (availabilityData is null)
                    return null;
                
                var supplier = settings.IsSupplierVisible
                    ? result.Supplier
                    : (Suppliers?) null;

                var isDirectContract = settings.IsDirectContractFlagVisible && availabilityData.Value.RoomContractSet.IsDirectContract;

                return availabilityDetails.Data.ToRoomContractSetAvailability(supplier, isDirectContract,
                    GetAvailablePaymentTypes(availabilityData.Value, contractKind), slimAccommodation);
            }


            Result<Unit, ProblemDetails> CheckAgainstSettings(RoomContractSetAvailability? availability)
            {
                if (availability is null)
                    return Unit.Instance;

                var availabilityValue = availability.Value;

                return RoomContractSetSettingsChecker.IsEvaluationAllowed(availabilityValue.RoomContractSet, availabilityValue.CheckInDate, settings, _dateTimeProvider) && availabilityValue.AvailablePaymentMethods.Any()
                    ? Unit.Instance
                    : ProblemDetailsBuilder.Fail<Unit>("You can't book the contract within deadline without explicit approval from a Happytravel.com officer.");
            }
            
            
            List<PaymentTypes> GetAvailablePaymentTypes(in EdoContracts.Accommodations.RoomContractSetAvailability availability,
                in CounterpartyContractKind contractKind)
                => BookingPaymentTypesHelper.GetAvailablePaymentTypes(availability, settings, contractKind, _dateTimeProvider.UtcNow());


            static SlimAccommodation GetSlimAccommodation(Accommodation accommodation)
            {
                var location = accommodation.Location;
                return new SlimAccommodation(id: accommodation.Id,
                    location: new SlimLocationInfo(address: location.Address,
                        country: location.Country,
                        countryCode: location.CountryCode,
                        locality: location.Locality,
                        localityZone: location.LocalityZone,
                        coordinates: location.Coordinates),
                    name: accommodation.Name,
                    photo: accommodation.Photos.FirstOrDefault(),
                    rating: accommodation.Rating,
                    propertyType: accommodation.Type,
                    htId: accommodation.HtId,
                    hotelChain: accommodation.HotelChain);
            }
        }
        
        
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IBookingEvaluationPriceProcessor _priceProcessor;
        private readonly IRoomSelectionStorage _roomSelectionStorage;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingEvaluationStorage _bookingEvaluationStorage;
        private readonly ICounterpartyService _counterpartyService;
        private readonly IAccommodationService _accommodationService;
        private readonly ILogger<BookingEvaluationService> _logger;
    }
}