using System;
using HappyTravel.Geography;

namespace HappyTravel.Edo.Data.Bookings
{
    public class AccommodationLocation
    {
        // EF constructor
        private AccommodationLocation() { }


        public AccommodationLocation(string country, string locality, string zone, string address, GeoPoint coordinates)
        {
            Country = country;
            Locality = locality;
            Zone = zone;
            Address = address;
            Coordinates = coordinates;
        }
        
        public string Country { get; set; }
        public string Locality { get; set; }
        public string Zone { get; set; }
        public string Address { get; set; }
        public GeoPoint Coordinates { get; set; }


        public bool Equals(AccommodationLocation other)
            => Country == other.Country && Locality == other.Locality && Zone == other.Zone && Address == other.Address &&
                Coordinates.Equals(other.Coordinates);


        public override bool Equals(object obj) => obj is AccommodationLocation other && Equals(other);


        public override int GetHashCode() => HashCode.Combine(Country, Locality, Zone, Address, Coordinates);
    }
}