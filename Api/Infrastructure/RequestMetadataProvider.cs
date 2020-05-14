using System.Globalization;
using HappyTravel.Edo.Api.Infrastructure.Http.Extensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public class RequestMetadataProvider
    {
        public RequestMetadataProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public RequestMetadata Get()
        {
            var requestId = _httpContextAccessor.HttpContext.Request.GetRequestId();
            var languageCode = CultureInfo.CurrentCulture.Name;
            return new RequestMetadata(requestId, languageCode);
        }
        
        private readonly IHttpContextAccessor _httpContextAccessor;
    }
}