namespace HappyTravel.Edo.Data.Bookings
{
    public class ImageInfo
    {
        // EF constructor
        private ImageInfo() { }


        public ImageInfo(string caption, string sourceUrl)
        {
            Caption = caption;
            SourceUrl = sourceUrl;
        }


        public string Caption { get; set; }
        public string SourceUrl { get; set; }
    }
}
