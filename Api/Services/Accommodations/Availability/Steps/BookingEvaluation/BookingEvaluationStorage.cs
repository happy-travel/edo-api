using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public class BookingEvaluationStorage : IBookingEvaluationStorage
    {
        public BookingEvaluationStorage(IDoubleFlow doubleFlow)
        {
            _doubleFlow = doubleFlow;
        }


        public Task Set(Guid searchId, Guid resultId, Guid roomContractSetId, DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> availability,
            DataProviders dataProvider)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            var dataToSave = ProviderData.Create(dataProvider, availability);
            return _doubleFlow.SetAsync(key, dataToSave, CacheExpirationTime);
        }


        public async Task<Result<(DataProviders Source, DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> Result)>> Get(Guid searchId, Guid resultId, Guid roomContractSetId, List<DataProviders> dataProviders)
        {
            var key = BuildKey(searchId, resultId, roomContractSetId);
            
            var result = await _doubleFlow.GetAsync<ProviderData<DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline>>>(key, CacheExpirationTime);
            return result.Equals(default)
                ? Result.Failure<(DataProviders, DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline>)>("Could not find evaluation result")
                : (result.Source, result.Data);
        }

        
        private string BuildKey(Guid searchId, Guid resultId, Guid roomContractSetId) => $"{searchId}::{resultId}::{roomContractSetId}";
        
        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromMinutes(15);
        private readonly IDoubleFlow _doubleFlow;
    }
}