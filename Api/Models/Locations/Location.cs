using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.GeoData.Enums;
using HappyTravel.Geography;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Locations
{
    public readonly struct Location
    {
        [JsonConstructor]
        public Location(string id, string name, string locality, string country, string countryCode, GeoPoint coordinates, int distance, PredictionSources source, LocationTypes type,
            List<Suppliers> suppliers)
        {
            Id = id;
            Name = name;
            Locality = locality;
            Country = country;
            CountryCode = countryCode;
            Coordinates = coordinates;
            Distance = distance;
            Source = source;
            Type = type;
            Suppliers = suppliers ?? new List<Suppliers>();
        }


        public Location(GeoPoint coordinates, int distance, LocationTypes type = LocationTypes.Unknown)
            : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, coordinates, distance, PredictionSources.NotSpecified, type, null)
        { }


        public Location(Location source, int distance) : this(source.Id, source.Name, source.Locality, source.Country, source.CountryCode, source.Coordinates, distance, source.Source, source.Type, source.Suppliers)
        { }
        
        
        public string Id { get; init; }
        public GeoPoint Coordinates { get; init;}
        public string Country { get; init;}
        public string CountryCode { get; init;}
        public int Distance { get; init;}
        public string Locality { get; init;}
        public string Name { get; init;}
        public PredictionSources Source { get; init;}
        public LocationTypes Type { get; init;}
        public List<Suppliers> Suppliers { get; init;}


        public override bool Equals(object obj) => obj is Location other && Equals(other);


        public bool Equals(Location other)
            => (Id, Coordinates, Coordinates, Country, Distance, Locality, Name, Source, Type) == (other.Id, other.Coordinates,
                other.Coordinates, other.Country, other.Distance, other.Locality, other.Name,
                other.Source, other.Type);


        public override int GetHashCode() => (Id, Coordinates, Country, Distance, Locality, Name, Source, Type).GetHashCode();
    }
}