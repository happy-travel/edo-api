using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(IMemoryFlow flow,
            IProviderRouter providerRouter)
        {
            _flow = flow;
            _providerRouter = providerRouter;
        }


        public ValueTask<Result<AccommodationDetails, ProblemDetails>> Get(DataProviders source, string accommodationId, RequestMetadata requestMetadata)
        {
            return _flow.GetOrSetAsync(_flow.BuildKey(nameof(AccommodationService), "Accommodations", requestMetadata.LanguageCode, accommodationId),
                async () => await _providerRouter.GetAccommodation(source, accommodationId, requestMetadata),
                TimeSpan.FromDays(1));
        }


        private readonly IMemoryFlow _flow;
        private readonly IProviderRouter _providerRouter;
    }
}