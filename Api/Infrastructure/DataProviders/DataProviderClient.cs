using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Infrastructure.DataProviders
{
    public class DataProviderClient : IDataProviderClient
    {
        public DataProviderClient(IHttpClientFactory clientFactory, ILogger<DataProviderClient> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _serializer = new JsonSerializer();
        }


        public Task<Result<T, ProblemDetails>> Get<T>(Uri url, RequestMetadata requestMetadata,
            CancellationToken cancellationToken = default)
            => Send<T>(new HttpRequestMessage(HttpMethod.Get, url), requestMetadata, cancellationToken);


        public Task<Result<TOut, ProblemDetails>> Post<T, TOut>(Uri url, T requestContent, RequestMetadata requestMetadata, 
            CancellationToken cancellationToken = default)
            => Send<TOut>(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = BuildContent(requestContent)
            }, requestMetadata, cancellationToken);


        public Task<Result<TOut, ProblemDetails>> Post<TOut>(Uri url, RequestMetadata requestMetadata, 
            CancellationToken cancellationToken = default)
            => Send<TOut>(new HttpRequestMessage(HttpMethod.Post, url), requestMetadata, cancellationToken);
        

        public Task<Result<VoidObject, ProblemDetails>> Post(Uri uri, RequestMetadata requestMetadata, 
            CancellationToken cancellationToken = default)
            => Post<VoidObject, VoidObject>(uri, VoidObject.Instance, requestMetadata, cancellationToken);


        public Task<Result<TOut, ProblemDetails>> Post<TOut>(Uri url, Stream stream, RequestMetadata requestMetadata, 
            CancellationToken cancellationToken = default)
            => Send<TOut>(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StreamContent(stream)
            }, requestMetadata, cancellationToken);
        
      
        private static StringContent BuildContent<T>(T requestContent)
            => requestContent is VoidObject
                ? null
                : new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");
        
            
        
        public async Task<Result<TResponse, ProblemDetails>> Send<TResponse>(HttpRequestMessage request, RequestMetadata requestMetadata, CancellationToken cancellationToken)
        {
            try
            {
                using var client = _clientFactory.CreateClient();
                
                client.DefaultRequestHeaders.Add("Accept-Language", requestMetadata.LanguageCode);
                client.DefaultRequestHeaders.Add(Constants.Common.RequestIdHeader, requestMetadata.RequestId);

                using var response = await client.SendAsync(request, cancellationToken);
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(stream);
                using var jsonTextReader = new JsonTextReader(streamReader);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = _serializer.Deserialize<ProblemDetails>(jsonTextReader) ??
                        ProblemDetailsBuilder.Build(response.ReasonPhrase, response.StatusCode);

                    return Result.Fail<TResponse, ProblemDetails>(error);
                }

                var availabilityResponse = _serializer.Deserialize<TResponse>(jsonTextReader);
                return Result.Ok<TResponse, ProblemDetails>(availabilityResponse);
            }
            catch (Exception ex)
            {
                ex.Data.Add("requested url", request.RequestUri);

                _logger.LogError(ex, "Http request failed");
                return ProblemDetailsBuilder.Fail<TResponse>(ex.Message);
            }
        }


        private readonly IHttpClientFactory _clientFactory;
        private readonly JsonSerializer _serializer;
        private readonly ILogger<DataProviderClient> _logger;
    }
}