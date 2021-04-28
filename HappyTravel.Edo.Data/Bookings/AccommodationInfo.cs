namespace HappyTravel.Edo.Data.Bookings
{
    public class AccommodationInfo
    {
        // EF constructor
        private AccommodationInfo() { }


        public AccommodationInfo(string id, string name, ImageInfo photo)
        {
            Id = id;
            Name = name;
            Photo = photo;
        }


        public string Id { get; set; }
        public string Name { get; set; }
        public ImageInfo Photo { get; set; }
    }
}
