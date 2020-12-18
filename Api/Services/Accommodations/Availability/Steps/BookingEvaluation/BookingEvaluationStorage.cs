using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using RoomContractSetAvailability = HappyTravel.EdoContracts.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingEvaluationStorage : IBookingEvaluationStorage
    {
        public BookingEvaluationStorage(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }


        public Task Set(Guid searchId, Guid resultId, Guid roomContractSetId, DataWithMarkup<RoomContractSetAvailability> availability,
            Suppliers supplier)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            var dataToSave = SupplierData.Create(supplier, availability);
            return _doubleFlow.SetAsync(key, dataToSave, CacheExpirationTime);
        }


        public async Task<Result<BookingAvailabilityInfo>> Get(Guid searchId, Guid resultId, Guid roomContractSetId)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            
            var result = await _doubleFlow.GetAsync<SupplierData<DataWithMarkup<RoomContractSetAvailability>>>(key, CacheExpirationTime);
            if (result.Equals(default))
                return Result.Failure<BookingAvailabilityInfo>("Could not find evaluation result");

            return ExtractBookingAvailabilityInfo(result);

        }
        
        
        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(SupplierData<DataWithMarkup<RoomContractSetAvailability>> dataWithMarkup)
        {
            var response = dataWithMarkup.Data.Data;
            var supplier = dataWithMarkup.Source;
            var location = response.Accommodation.Location;

            return new BookingAvailabilityInfo(
                response.Accommodation.Id,
                response.Accommodation.Name,
                response.RoomContractSet.ToRoomContractSet(supplier),
                location.LocalityZone,
                location.Locality,
                location.Country,
                location.CountryCode,
                location.Address,
                location.Coordinates,
                response.CheckInDate,
                response.CheckOutDate,
                response.NumberOfNights,
                supplier,
                dataWithMarkup.Data.AppliedMarkups,
                dataWithMarkup.Data.SupplierPrice,
                dataWithMarkup.Data.Data.AvailabilityId);
        }

        
        private string BuildKey(Guid searchId, Guid resultId, Guid roomContractSetId) => $"{searchId}::{resultId}::{roomContractSetId}";
        
        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromMinutes(15);
        private readonly IDoubleFlow _doubleFlow;
    }
}