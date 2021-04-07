using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Options;
using RoomContractSetAvailability = HappyTravel.EdoContracts.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingEvaluationStorage : IBookingEvaluationStorage
    {
        public BookingEvaluationStorage(IDoubleFlow doubleFlow, IOptions<BookingOptions> bookingOptions)
        {
            _doubleFlow = doubleFlow;
            _bookingOptions = bookingOptions.Value;
        }


        public Task Set(Guid searchId, Guid resultId, Guid roomContractSetId, DataWithMarkup<RoomContractSetAvailability> availability,
            Suppliers supplier, List<PaymentMethods> availablePaymentMethods, string htId)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            var result = SupplierData.Create(supplier, availability);
            var roomSetAvailability = availability.Data;
            
            var location = roomSetAvailability.Accommodation.Location;
            var roomContractSet = roomSetAvailability.RoomContractSet.ToRoomContractSet(result.Source,
                roomSetAvailability.RoomContractSet.IsDirectContract, _bookingOptions.CreditCardPaymentCommission);
            
            var dataWithMarkup = result.Data;
            
            var bookingAvailabilityInfo = new BookingAvailabilityInfo(
                roomSetAvailability.Accommodation.Id,
                roomSetAvailability.Accommodation.Name,
                roomContractSet,
                location.LocalityZone,
                location.Locality,
                location.Country,
                location.CountryCode,
                location.Address,
                location.Coordinates,
                roomSetAvailability.CheckInDate,
                roomSetAvailability.CheckOutDate,
                roomSetAvailability.NumberOfNights,
                result.Source,
                dataWithMarkup.AppliedMarkups,
                dataWithMarkup.ConvertedSupplierPrice,
                dataWithMarkup.OriginalSupplierPrice,
                roomSetAvailability.AvailabilityId,
                htId,
                availablePaymentMethods,
                roomSetAvailability.RoomContractSet.IsDirectContract);
            
            return _doubleFlow.SetAsync(key, bookingAvailabilityInfo, CacheExpirationTime);
        }


        public async Task<Result<BookingAvailabilityInfo>> Get(Guid searchId, Guid resultId, Guid roomContractSetId)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            
            var result = await _doubleFlow.GetAsync<BookingAvailabilityInfo>(key, CacheExpirationTime);
            return result.Equals(default) 
                ? Result.Failure<BookingAvailabilityInfo>("Could not find evaluation result") 
                : result;
        }

        
        private string BuildKey(Guid searchId, Guid resultId, Guid roomContractSetId) 
            => _doubleFlow.BuildKey(searchId.ToString(), resultId.ToString(), roomContractSetId.ToString());
        
        
        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromMinutes(15);
        private readonly IDoubleFlow _doubleFlow;
        private readonly BookingOptions _bookingOptions;
    }
}