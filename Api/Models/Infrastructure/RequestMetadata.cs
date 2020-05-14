namespace HappyTravel.Edo.Api.Models.Infrastructure
{
    public readonly struct RequestMetadata
    {
        public RequestMetadata(string requestId, string languageCode)
        {
            RequestId = requestId;
            LanguageCode = languageCode;
        }


        public string RequestId { get; }
        public string LanguageCode { get; }
    }
}