using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public class DataProviderClient : IDataProviderClient
    {
        public DataProviderClient(IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
        {
            _clientFactory = clientFactory;
            _logger = loggerFactory.CreateLogger<DataProviderClient>();

            _serializer = new JsonSerializer();
        }


        public Task<Result<T, ProblemDetails>> Get<T>(Uri url, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default)
            => Send<T>(new HttpRequestMessage(HttpMethod.Get, url), languageCode, cancellationToken);


        public Task<Result<TOut, ProblemDetails>> Post<T, TOut>(Uri url, T requestContent, string languageCode = LocalizationHelper.DefaultLanguageCode, CancellationToken cancellationToken = default)
            => Send<TOut>(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = BuildContent(requestContent)
            }, languageCode, cancellationToken);

        private static StringContent BuildContent<T>(T requestContent)
        {
            var content = requestContent is VoidObject
                ? string.Empty
                : JsonConvert.SerializeObject(requestContent);
            
            return new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        }
            
        private async Task<Result<TResponse,ProblemDetails>> Send<TResponse>(HttpRequestMessage request, string languageCode, CancellationToken cancellationToken)
        {
            try
            {
                using (var client = _clientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Add("Accept-Language", languageCode);
                    
                    using (var response = await client.SendAsync(request, cancellationToken))
                    using (var stream = await GetResponseStream(response) )
                    using (var streamReader = new StreamReader(stream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            var error = _serializer.Deserialize<ProblemDetails>(jsonTextReader);
                            return Result.Fail<TResponse, ProblemDetails>(error);
                        }

                        var availabilityResponse = _serializer.Deserialize<TResponse>(jsonTextReader);
                        return Result.Ok<TResponse, ProblemDetails>(availabilityResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("requested url", request.RequestUri);

                _logger.LogDataProviderClientException(ex);
                return ProblemDetailsBuilder.Fail<TResponse>(ex.Message);
            }

            Task<Stream> GetResponseStream(HttpResponseMessage response)
            {
                return typeof(TResponse) == typeof(VoidObject)
                    ? Task.FromResult(Stream.Null)
                    : response.Content.ReadAsStreamAsync();
            }
        }
    
        
        private readonly IHttpClientFactory _clientFactory;
        private readonly JsonSerializer _serializer;
        private readonly ILogger<DataProviderClient> _logger;
    }
}
