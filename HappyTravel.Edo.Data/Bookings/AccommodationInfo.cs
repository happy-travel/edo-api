namespace HappyTravel.Edo.Data.Bookings
{
    public class AccommodationInfo
    {
        // EF constructor
        private AccommodationInfo() { }


        public AccommodationInfo(ImageInfo photo)
        {
            Photo = photo;
        }


        public ImageInfo Photo { get; set; }
    }
}
