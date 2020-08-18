using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(IDoubleFlow flow,
            IDataProviderFactory dataProviderFactory)
        {
            _flow = flow;
            _dataProviderFactory = dataProviderFactory;
        }


        public Task<Result<AccommodationDetails, ProblemDetails>> Get(DataProviders source, string accommodationId, string languageCode)
        {
            return _flow.GetOrSetAsync(_flow.BuildKey(nameof(AccommodationService), nameof(Get), languageCode, accommodationId),
                async () => await _dataProviderFactory.Get(source).GetAccommodation(accommodationId, languageCode),
                TimeSpan.FromDays(1));
        }


        private readonly IDoubleFlow _flow;
        private readonly IDataProviderFactory _dataProviderFactory;
    }
}