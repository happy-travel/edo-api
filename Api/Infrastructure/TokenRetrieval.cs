using System;
using Microsoft.AspNetCore.Http;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public static class TokenRetrieval
    {
        /// <summary>
        /// Reads the token from the authorization header and from query string for SignalR hubs.
        /// </summary>
        /// <returns></returns>
        public static Func<HttpRequest, string> FromAuthorizationHeaderOrQueryString()
        {
            return (request) =>
            {
                var func = !request.Path.StartsWithSegments("/signalr")
                    ? IdentityModel.AspNetCore.OAuth2Introspection.TokenRetrieval.FromAuthorizationHeader()
                    : IdentityModel.AspNetCore.OAuth2Introspection.TokenRetrieval.FromQueryString();

                return func(request);
            };
        }
    }
}