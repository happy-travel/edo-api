using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.GeoData.Enums;
using NetTopologySuite.Geometries;

namespace HappyTravel.Edo.Data.Locations
{
    public class Location
    {
        public Guid Id { get; set; }
        public string Country { get; set; }
        public Point Coordinates { get; set; } = new Point(0, 0);
        public int DistanceInMeters { get; set; }
        public string Locality { get; set; }
        public string Name { get; set; }
        public PredictionSources Source { get; set; }
        public LocationTypes Type { get; set; }
        public List<Common.Enums.Suppliers> Suppliers { get; set; }
        public DateTime Modified { get; set; }
        public string DefaultName { get; set; }
        public string DefaultCountry { get; set; }
        public string DefaultLocality { get; set; }
    }
}