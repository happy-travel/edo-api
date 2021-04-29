using HappyTravel.EdoContracts.Accommodations.Internals;
using Newtonsoft.Json;
using System;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct AccommodationInfo
    {
        [JsonConstructor]
        public AccommodationInfo(ImageInfo photo)
        {
            Photo = photo;
        }


        /// <summary>
        ///     Accommodation photo.
        /// </summary>
        public ImageInfo Photo { get; }


        public bool Equals(AccommodationInfo other)
            => Photo.Equals(other.Photo);


        public override bool Equals(object obj) => obj is AccommodationInfo other && Equals(other);


        public override int GetHashCode()
            => Photo.GetHashCode();
    }
}
