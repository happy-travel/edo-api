using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Models.Locations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Infrastructure.Locations
{
    public class LocationClient : ILocationClient
    {
        public LocationClient(IHttpClientFactory clientFactory, IOptions<JsonOptions> jsonOptions)
        {
            _clientFactory = clientFactory;
            _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
        }


        public Task<Result<Location>> Get(string id, string languageCode, CancellationToken cancellationToken = default) 
            => Execute<Location>(new HttpRequestMessage(HttpMethod.Get, $"locations/{id}"), languageCode, cancellationToken);


        public Task<Result<List<Location>>> Search(string query, string languageCode, int skip = 0, int top = 10, CancellationToken cancellationToken = default)
            => Execute<List<Location>>(new HttpRequestMessage(HttpMethod.Get, $"locations/?query={query}"), languageCode, cancellationToken);
        

        private async Task<Result<TResponse>> Execute<TResponse>(HttpRequestMessage requestMessage, string languageCode, CancellationToken cancellationToken = default)
        {
            using var client = _clientFactory.CreateClient(HttpClientNames.Locations);
            client.DefaultRequestHeaders.Add("Accept-Language", languageCode);
            var responseMessage = await client.SendAsync(requestMessage, cancellationToken);
            var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);

            if (responseMessage.IsSuccessStatusCode)
                return await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonSerializerOptions, cancellationToken);

            var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(stream, _jsonSerializerOptions, cancellationToken);//  responseMessage.ReasonPhrase responseMessage.StatusCode;

            var error = problemDetails is null 
                ? $"Reason {responseMessage.ReasonPhrase}, Code: {responseMessage.StatusCode}" 
                : problemDetails.Detail;

            return Result.Failure<TResponse>(error);
        }


        private readonly JsonSerializerOptions _jsonSerializerOptions; 
        private readonly IHttpClientFactory _clientFactory;
    }
}