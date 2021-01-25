using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Locations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace HappyTravel.Edo.Api.Services.Locations
{
    public class InteriorGeoCoder : IGeoCoder
    {
        public InteriorGeoCoder(ICountryService countryService, IWebHostEnvironment environment, IAccommodationBookingSettingsService accommodationBookingSettingsService, ILocationClient locationClient)
        {
            _countryService = countryService;
            _environment = environment;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
            _locationClient = locationClient;
        }


        public async Task<Result<Location>> GetLocation(SearchLocation searchLocation, string languageCode)
        {
            var id = searchLocation.PredictionResult.Id;

            var (_, isFailure, location, error)  = await _locationClient.Get(id, languageCode);

            if (isFailure)
                return Result.Failure<Location>($"Failed to get location with ID {searchLocation.PredictionResult.Id}. {error}");
           
            location = searchLocation.DistanceInMeters != 0 
                ? new Location(location, searchLocation.DistanceInMeters) 
                : location;

            return Result.Success(location);
        }


        public async ValueTask<Result<List<Prediction>>> GetLocationPredictions(string query, string sessionId, AgentContext agent, string languageCode)
        {
            var (_, isFailure, locations, error) = await _locationClient.Search(query, languageCode, 0, MaximumNumberOfPredictions);
            if (isFailure)
                return Result.Failure<List<Prediction>>(error);
            
            var enabledSuppliers = (await _accommodationBookingSettingsService.Get(agent)).EnabledConnectors;

            var predictions = new List<Prediction>(locations.Count);
            foreach (var location in locations)
            {
                if (_environment.IsProduction())
                {
                    if (IsRestricted(location, agent.AgentId))
                        continue;
                }
                
                if (!enabledSuppliers.Intersect(location.Suppliers).Any())
                    continue;
                
                var predictionValue = BuildPredictionValue(location);
                
                predictions.Add(new Prediction(location.Id, location.CountryCode, location.Source, location.Type, predictionValue));
            }

            return Result.Success(predictions);
        }


        private static bool IsRestricted(Location location, int agentId)
        {
            if (agentId != DemoAccountId)
                return false;

            if (RestrictedCountries.Contains(location.Country))
                return true;

            if (RestrictedLocalities.Contains(location.Locality))
                return true;

            return false;
        }


        private static string BuildPredictionValue(Location location)
        {
            var result = location.Name;
            
            if (!string.IsNullOrEmpty(location.Locality))
                result += string.IsNullOrEmpty(result) ? location.Locality : ", " + location.Locality;
                
            if (!string.IsNullOrEmpty(location.Country))
                result += string.IsNullOrEmpty(result)? location.Country : ", " + location.Country;

            return result;
        }


        internal static readonly int DemoAccountId = 93;
        private static readonly HashSet<string> RestrictedCountries = new()
        {
            "BAHRAIN",
            "KUWAIT"
        };
        private static readonly HashSet<string> RestrictedLocalities = new()
        {
            "AMMAN",
            "CAPE TOWN",
            "JEDDAH",
            "KUWAIT CITY",
            "LANGKAWI",
            "MAKKAH",
            "MARRAKECH",
            "MEDINA",
            "SHARM EL SHEIKH"
        };

        private const int MaximumNumberOfPredictions = 10;

        private readonly ICountryService _countryService;
        private readonly IWebHostEnvironment _environment;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly ILocationClient _locationClient;
    }
}