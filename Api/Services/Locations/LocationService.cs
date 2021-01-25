using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.GeoData.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace HappyTravel.Edo.Api.Services.Locations
{
    public class LocationService : ILocationService
    {
        public LocationService(EdoContext context, IDoubleFlow flow, IEnumerable<IGeoCoder> geoCoders,
            GeometryFactory geometryFactory, IOptions<LocationServiceOptions> options, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _flow = flow;
            _geometryFactory = geometryFactory;

            _googleGeoCoder = geoCoders.First(c => c is GoogleGeoCoder);
            _interiorGeoCoder = geoCoders.First(c => c is InteriorGeoCoder);

            _countryService = new CountryService(context, flow);
            _options = options.Value;

            _dateTimeProvider = dateTimeProvider;
        }


        public async ValueTask<Result<Models.Locations.Location, ProblemDetails>> Get(SearchLocation searchLocation, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(searchLocation.PredictionResult.Id))
                return Result.Success<Models.Locations.Location, ProblemDetails>(new Models.Locations.Location(searchLocation.Coordinates,
                    searchLocation.DistanceInMeters));

            if (searchLocation.PredictionResult.Type == LocationTypes.Unknown)
                return ProblemDetailsBuilder.Fail<Models.Locations.Location>(
                    "Invalid prediction type. It looks like a prediction type was not specified in the request.");

            var cacheKey = _flow.BuildKey(nameof(LocationService), GeoCoderKey, searchLocation.PredictionResult.Source.ToString(),
                searchLocation.PredictionResult.Id);
            
            if (_flow.TryGetValue(cacheKey, out Models.Locations.Location result, DefaultLocationCachingTime))
                return Result.Success<Models.Locations.Location, ProblemDetails>(result);

            Result<Models.Locations.Location> locationResult;
            switch (searchLocation.PredictionResult.Source)
            {
                case PredictionSources.Google:
                    locationResult = await _googleGeoCoder.GetLocation(searchLocation, languageCode);
                    break;
                case PredictionSources.Interior:
                    locationResult = await _interiorGeoCoder.GetLocation(searchLocation, languageCode);
                    break;
                // ReSharper disable once RedundantCaseLabel
                case PredictionSources.NotSpecified:
                default:
                    locationResult =
                        Result.Failure<Models.Locations.Location>(
                            $"'{nameof(searchLocation.PredictionResult.Source)}' is empty or wasn't specified in your request.");
                    break;
            }

            if (locationResult.IsFailure)
                return ProblemDetailsBuilder.Fail<Models.Locations.Location>(locationResult.Error, HttpStatusCode.ServiceUnavailable);

            result = locationResult.Value;
            await _flow.SetAsync(cacheKey, result, DefaultLocationCachingTime);

            return Result.Success<Models.Locations.Location, ProblemDetails>(result);
        }


        public Task<List<Country>> GetCountries(string query, string languageCode) => _countryService.Get(query, languageCode);


        public async ValueTask<Result<List<Prediction>, ProblemDetails>> GetPredictions(string query, string sessionId, AgentContext agent, string languageCode)
        {
            query = query?.Trim().ToLowerInvariant();
            if (query == null || query.Length < 3)
                return Result.Success<List<Prediction>, ProblemDetails>(new List<Prediction>(0));

            var cacheKey = agent.AgentId == InteriorGeoCoder.DemoAccountId
                ? _flow.BuildKey(nameof(LocationService), PredictionsKeyBase, languageCode, query)
                : _flow.BuildKey(nameof(LocationService), PredictionsKeyBase, agent.AgentId.ToString(), agent.AgencyId.ToString(), languageCode, query);

            if (_flow.TryGetValue(cacheKey, out List<Prediction> predictions, DefaultLocationCachingTime))
                return Result.Success<List<Prediction>, ProblemDetails>(predictions);

            (_, _, predictions, _) = await _interiorGeoCoder.GetLocationPredictions(query, sessionId, agent, languageCode);

            if (_options.IsGoogleGeoCoderDisabled || DesirableNumberOfLocalPredictions < predictions.Count)
            {
                await _flow.SetAsync(cacheKey, predictions, DefaultLocationCachingTime);
                return Result.Success<List<Prediction>, ProblemDetails>(predictions);
            }

            var (_, isFailure, googlePredictions, error) = await _googleGeoCoder.GetLocationPredictions(query, sessionId, default, languageCode);
            if (isFailure && !predictions.Any())
                return ProblemDetailsBuilder.Fail<List<Prediction>>(error);

            if (googlePredictions != null)
                predictions.AddRange(SortPredictions(googlePredictions));

            await _flow.SetAsync(cacheKey, predictions, DefaultLocationCachingTime);

            return Result.Success<List<Prediction>, ProblemDetails>(predictions);
        }


        public Task<List<Region>> GetRegions(string languageCode)
            => _flow.GetOrSetAsync(_flow.BuildKey(nameof(LocationService), RegionsKeyBase, languageCode), async ()
                => (await _context.Regions.ToListAsync())
                .Select(r => new Region(r.Id, LocalizationHelper.GetValueFromSerializedString(r.Names, languageCode))).ToList(), DefaultLocationCachingTime);


        private static TimeSpan DefaultLocationCachingTime => TimeSpan.FromDays(1);


        private static List<Prediction> SortPredictions(List<Prediction> target)
            => target.OrderBy(p => Array.IndexOf(PredictionTypeSortOrder, p.Type))
                .ThenBy(p => Array.IndexOf(PredictionSourceSortOrder, p.Source))
                .ToList();


        private const string GeoCoderKey = "GeoCoder";
        private const string PredictionsKeyBase = "Predictions";
        private const string RegionsKeyBase = "Regions";

        private const int DesirableNumberOfLocalPredictions = 5;

        private static readonly PredictionSources[] PredictionSourceSortOrder =
        {
            PredictionSources.Interior,
            PredictionSources.Google,
            PredictionSources.NotSpecified
        };

        private static readonly LocationTypes[] PredictionTypeSortOrder =
        {
            LocationTypes.Accommodation,
            LocationTypes.Location,
            LocationTypes.Destination,
            LocationTypes.Landmark,
            LocationTypes.Unknown
        };

        private readonly EdoContext _context;
        private readonly CountryService _countryService;
        private readonly IDoubleFlow _flow;
        private readonly GeometryFactory _geometryFactory;
        private readonly IGeoCoder _googleGeoCoder;
        private readonly IGeoCoder _interiorGeoCoder;
        private readonly LocationServiceOptions _options;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}