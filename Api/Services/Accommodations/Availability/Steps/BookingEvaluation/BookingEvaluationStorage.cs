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
            Suppliers supplier, List<PaymentTypes> availablePaymentTypes, string htId)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            var result = SupplierData.Create(supplier, availability);
            var roomSetAvailability = availability.Data;
            
            var location = roomSetAvailability.Accommodation.Location;
            var roomContractSet = roomSetAvailability.RoomContractSet.ToRoomContractSet(result.Source,
                roomSetAvailability.RoomContractSet.IsDirectContract, _bookingOptions.CreditCardPaymentCommission);
            
            var dataWithMarkup = result.Data;
            
            var bookingAvailabilityInfo = new BookingAvailabilityInfo(
                accommodationId: roomSetAvailability.Accommodation.Id,
                accommodationName: roomSetAvailability.Accommodation.Name,
                accommodationInfo: new AccommodationInfo(roomSetAvailability.Accommodation.Photo),
                roomContractSet: roomContractSet,
                zoneName: location.LocalityZone,
                localityName: location.Locality,
                countryName: location.Country,
                countryCode: location.CountryCode,
                address: location.Address,
                coordinates: location.Coordinates,
                checkInDate: roomSetAvailability.CheckInDate,
                checkOutDate: roomSetAvailability.CheckOutDate,
                numberOfNights: roomSetAvailability.NumberOfNights,
                supplier: result.Source,
                appliedMarkups: dataWithMarkup.AppliedMarkups,
                convertedSupplierPrice: dataWithMarkup.ConvertedSupplierPrice,
                originalSupplierPrice: dataWithMarkup.OriginalSupplierPrice,
                availabilityId: roomSetAvailability.AvailabilityId,
                htId: htId,
                availablePaymentTypes: availablePaymentTypes,
                isDirectContract: roomSetAvailability.RoomContractSet.IsDirectContract);
            
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
    }
}