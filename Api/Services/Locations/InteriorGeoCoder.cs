using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Data;
using HappyTravel.Geography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace HappyTravel.Edo.Api.Services.Locations
{
    public class InteriorGeoCoder : IGeoCoder
    {
        public InteriorGeoCoder(EdoContext context, ICountryService countryService, IWebHostEnvironment environment, IAccommodationBookingSettingsService accommodationBookingSettingsService)
        {
            _context = context;
            _countryService = countryService;
            _environment = environment;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
        }


        public async Task<Result<Location>> GetLocation(SearchLocation searchLocation, string languageCode)
        {
            var id = Guid.Parse(searchLocation.PredictionResult.Id);

            var location = await _context.Locations
                .Where(l => l.Id == id)
                .Select(l => new Location(l.Name, l.Locality, l.Country, new GeoPoint(l.Coordinates), l.DistanceInMeters, l.Source, l.Type, l.Suppliers))
                .FirstOrDefaultAsync();

            if (location.Equals(default))
                return Result.Failure<Location>($"No location with ID {searchLocation.PredictionResult.Id} has been found.");

            var name = location.Name.Length <= MinimalJsonFieldLength
                ? string.Empty
                : LocalizationHelper.GetValueFromSerializedString(location.Name, languageCode);

            var locality = location.Locality.Length <= MinimalJsonFieldLength
                ? string.Empty
                : LocalizationHelper.GetValueFromSerializedString(location.Locality, languageCode);

            var country = LocalizationHelper.GetValueFromSerializedString(location.Country, languageCode);
            var distance = searchLocation.DistanceInMeters != 0 ? searchLocation.DistanceInMeters : location.Distance;

            return Result.Success(new Location(name, locality, country, location.Coordinates, distance, location.Source, location.Type, location.Suppliers));
        }


        public async ValueTask<Result<List<Prediction>>> GetLocationPredictions(string query, string sessionId, AgentContext agent, string languageCode)
        {
            var locations = await _context.SearchLocations(query, MaximumNumberOfPredictions).ToListAsync();
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
                
                var predictionValue = BuildPredictionValue(location, languageCode);

                var countryName = LocalizationHelper.GetValueFromSerializedString(location.Country, LocalizationHelper.DefaultLanguageCode);
                var countryCode = await _countryService.GetCode(countryName, languageCode);

                predictions.Add(new Prediction(location.Id.ToString("N"), countryCode, location.Source, location.Type, predictionValue));
            }

            return Result.Success(predictions);
        }


        private static bool IsRestricted(Data.Locations.Location location, int agentId)
        {
            if (agentId != DemoAccountId)
                return false;

            var countryName = LocalizationHelper.GetValueFromSerializedString(location.Country, LocalizationHelper.DefaultLanguageCode);
            if (RestrictedCountries.Contains(countryName))
                return true;

            var localityName = LocalizationHelper.GetValueFromSerializedString(location.Locality, LocalizationHelper.DefaultLanguageCode);
            if (RestrictedLocalities.Contains(localityName))
                return true;

            return false;
        }


        private static string BuildPredictionValue(Data.Locations.Location location, string languageCode)
        {
            var result = string.Empty;
            if (MinimalJsonFieldLength < location.Name.Length)
            {
                var name = LocalizationHelper.GetValueFromSerializedString(location.Name, languageCode);
                if (name != string.Empty)
                    result = name;
            }

            if (MinimalJsonFieldLength < location.Locality.Length)
            {
                var locality = LocalizationHelper.GetValueFromSerializedString(location.Locality, languageCode);
                if (locality != string.Empty)
                {
                    if (result != string.Empty)
                        result += ", " + locality;
                    else
                        result = locality;
                }
            }

            var country = LocalizationHelper.GetValueFromSerializedString(location.Country, languageCode);
            if (result != string.Empty)
                result += ", " + country;
            else
                result = country;

            return result;
        }


        internal static readonly int DemoAccountId = 93;
        private static readonly HashSet<string> RestrictedCountries = new HashSet<string>
        {
            "BAHRAIN",
            "KUWAIT"
        };
        private static readonly HashSet<string> RestrictedLocalities = new HashSet<string>
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
        private static readonly int MinimalJsonFieldLength = Infrastructure.Constants.Common.EmptyJsonFieldValue.Length;

        private readonly EdoContext _context;
        private readonly ICountryService _countryService;
        private readonly IWebHostEnvironment _environment;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
    }
}