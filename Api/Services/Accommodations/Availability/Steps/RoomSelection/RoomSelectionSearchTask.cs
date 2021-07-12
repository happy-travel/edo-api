using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Metrics;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.SuppliersCatalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public class RoomSelectionSearchTask
    {
        private RoomSelectionSearchTask(IRoomSelectionPriceProcessor priceProcessor,
            ISupplierConnectorManager supplierConnectorManager,
            IRoomSelectionStorage roomSelectionStorage)
        {
            _priceProcessor = priceProcessor;
            _supplierConnectorManager = supplierConnectorManager;
            _roomSelectionStorage = roomSelectionStorage;
        }


        public static RoomSelectionSearchTask Create(IServiceProvider serviceProvider) 
            => new(serviceProvider.GetRequiredService<IRoomSelectionPriceProcessor>(),
            serviceProvider.GetRequiredService<ISupplierConnectorManager>(),
            serviceProvider.GetRequiredService<IRoomSelectionStorage>()
        );


        public async Task<Result<SupplierData<AccommodationAvailability>, ProblemDetails>> GetSupplierAvailability(Guid searchId,
            string htId, Suppliers supplier, string supplierAccommodationCode, string availabilityId, AccommodationBookingSettings settings,
            AgentContext agent, string languageCode)
        {
            return await ExecuteRequest()
                .Bind(ReplaceAccommodationData)
                .Bind(ConvertCurrencies)
                .Map(ProcessPolicies)
                .Map(ApplyMarkups)
                .Map(AlignPrices)
                .Map(AddSupplierData)
                .Tap(SaveToCache);


            Task<Result<AccommodationAvailability, ProblemDetails>> ExecuteRequest()
            {
                using var timer = Counters.SupplierSearchResponseTimeDuration.WithLabels("room_selection_request", supplier.ToString()).NewTimer();
                return _supplierConnectorManager.Get(supplier).GetAvailability(availabilityId, supplierAccommodationCode, languageCode);
            }


            Result<AccommodationAvailability, ProblemDetails> ReplaceAccommodationData(AccommodationAvailability availabilityDetails)
            {
                return new AccommodationAvailability(availabilityId: availabilityDetails.AvailabilityId, 
                    accommodationId: supplierAccommodationCode,
                    checkInDate: availabilityDetails.CheckInDate,
                    checkOutDate: availabilityDetails.CheckOutDate,
                    numberOfNights: availabilityDetails.NumberOfNights,
                    roomContractSets: availabilityDetails.RoomContractSets);
            }


            Task<Result<AccommodationAvailability, ProblemDetails>> ConvertCurrencies(AccommodationAvailability availabilityDetails)
                => _priceProcessor.ConvertCurrencies(availabilityDetails, agent);


            AccommodationAvailability ProcessPolicies(AccommodationAvailability availabilityDetails)
                => RoomSelectionPolicyProcessor.Process(availabilityDetails, settings.CancellationPolicyProcessSettings);


            Task<AccommodationAvailability> ApplyMarkups(AccommodationAvailability response) 
                => _priceProcessor.ApplyMarkups(response, agent);
            
            
            async Task<AccommodationAvailability> AlignPrices(AccommodationAvailability response) 
                => await _priceProcessor.AlignPrices(response);


            SupplierData<AccommodationAvailability> AddSupplierData(AccommodationAvailability availabilityDetails)
                => SupplierData.Create(supplier, availabilityDetails);
            
            
            Task SaveToCache(SupplierData<AccommodationAvailability> details)
            {
                var availabilityData = details.Data;
                var result = new SingleAccommodationAvailability(availabilityData.AvailabilityId,
                    availabilityData.CheckInDate,
                    availabilityData.RoomContractSets,
                    htId);
                
                return _roomSelectionStorage.SaveResult(searchId, htId, result, details.Source);
            }
        }
        
        
        private readonly IRoomSelectionPriceProcessor _priceProcessor;
        private readonly ISupplierConnectorManager _supplierConnectorManager;
        private readonly IRoomSelectionStorage _roomSelectionStorage;
    }
}