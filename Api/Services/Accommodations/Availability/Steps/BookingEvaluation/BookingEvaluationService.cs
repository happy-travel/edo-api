using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using Microsoft.AspNetCore.Mvc;
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
            IBookingEvaluationStorage bookingEvaluationStorage)
        {
            _supplierConnectorManager = supplierConnectorManager;
            _priceProcessor = priceProcessor;
            _roomSelectionStorage = roomSelectionStorage;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _dateTimeProvider = dateTimeProvider;
            _bookingEvaluationStorage = bookingEvaluationStorage;
        }
        
        public async Task<Result<RoomContractSetAvailability?, ProblemDetails>> GetExactAvailability(
            Guid searchId, Guid resultId, Guid roomContractSetId, AgentContext agent, string languageCode)
        {
            var settings = await _accommodationBookingSettingsService.Get(agent);
            var (_, isFailure, result, error) = await GetSelectedRoomSet(searchId, resultId, roomContractSetId);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<RoomContractSetAvailability?>(error);
            
            return await EvaluateOnConnector(result)
                .Bind(CheckAgainstSettings)
                .Bind(ConvertCurrencies)
                .Map(ProcessPolicies)
                .Map(ApplyMarkups)
                .Tap(SaveToCache)
                .Map(ToDetails);


            async Task<Result<(Suppliers Supplier, RoomContractSet, string)>> GetSelectedRoomSet(Guid searchId, Guid resultId, Guid roomContractSetId)
            {
                var result = (await _roomSelectionStorage.GetResult(searchId, resultId, settings.EnabledConnectors))
                    .SelectMany(r =>
                    {
                        return r.Result.RoomContractSets
                            .Select(rs => (Source: r.Supplier, RoomContractSet: rs, r.Result.AvailabilityId));
                    })
                    .SingleOrDefault(r => r.RoomContractSet.Id == roomContractSetId);

                if (result.Equals(default))
                    return Result.Failure<(Suppliers, RoomContractSet, string)>("Could not find selected room contract set");
                
                return result;
            }

            
            Task<Result<EdoContracts.Accommodations.RoomContractSetAvailability?, ProblemDetails>> EvaluateOnConnector((Suppliers, RoomContractSet, string) selectedSet)
            {
                var (supplier, roomContractSet, availabilityId) = selectedSet;
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
                var supplierPrice = response?.RoomContractSet.Rate.FinalPrice.Amount ?? default;
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
                return DataWithMarkup.Create(responseWithMarkups, appliedMarkups, supplierPrice);
            }

            
            Task SaveToCache(DataWithMarkup<EdoContracts.Accommodations.RoomContractSetAvailability?> responseWithDeadline)
            {
                if (!responseWithDeadline.Data.HasValue)
                    return Task.CompletedTask;

                // TODO: Check that this id will not change on all connectors NIJO-823
                var finalRoomContractSetId = responseWithDeadline.Data.Value.RoomContractSet.Id;
                return _bookingEvaluationStorage.Set(searchId, resultId, finalRoomContractSetId, DataWithMarkup.Create(responseWithDeadline.Data.Value, 
                    responseWithDeadline.AppliedMarkups, responseWithDeadline.SupplierPrice), result.Supplier);
            }


            RoomContractSetAvailability? ToDetails(DataWithMarkup<EdoContracts.Accommodations.RoomContractSetAvailability?> availabilityDetails)
            {
                var supplier = settings.IsSupplierVisible
                    ? result.Supplier
                    : (Suppliers?) null;
                
                return availabilityDetails.Data.ToRoomContractSetAvailability(supplier);
            }


            Result<EdoContracts.Accommodations.RoomContractSetAvailability?, ProblemDetails> CheckAgainstSettings(EdoContracts.Accommodations.RoomContractSetAvailability? availability)
            {
                if (availability is null)
                    return (EdoContracts.Accommodations.RoomContractSetAvailability?)null;

                if (settings.AprMode == AprMode.Hide || settings.AprMode == AprMode.DisplayOnly)
                {
                    if (availability.Value.RoomContractSet.IsAdvancePurchaseRate)
                        return ProblemDetailsBuilder.Fail<EdoContracts.Accommodations.RoomContractSetAvailability?>("You can't book the restricted contract without explicit approval from a Happytravel.com officer.");
                }
                    
                if (settings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.Hide ||
                    settings.PassedDeadlineOffersMode == PassedDeadlineOffersMode.DisplayOnly)
                {
                    var deadlineDate = availability.Value.RoomContractSet.Deadline.Date;
                    if (deadlineDate.HasValue && deadlineDate.Value.Date <= _dateTimeProvider.UtcTomorrow())
                        return ProblemDetailsBuilder.Fail<EdoContracts.Accommodations.RoomContractSetAvailability?>("You can't book the contract within deadline without explicit approval from a Happytravel.com officer.");
                }

                return availability;
            }
        }
        
        
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IBookingEvaluationPriceProcessor _priceProcessor;
        private readonly IRoomSelectionStorage _roomSelectionStorage;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingEvaluationStorage _bookingEvaluationStorage;
    }
}