using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace HappyTravel.Edo.Api.Infrastructure.SupplierConnectors
{
    public class ConnectorClient : IConnectorClient
    {
        public ConnectorClient(IHttpClientFactory clientFactory, 
            IConnectorSecurityTokenManager securityTokenManager,
            ILogger<ConnectorClient> logger)
        {
            _clientFactory = clientFactory;
            _securityTokenManager = securityTokenManager;
            _logger = logger;
            _serializer = new JsonSerializer
            {
                Converters = {new ProblemDetailsConverter()}
            };
        }


        public Task<Result<TResponse, ProblemDetails>> Get<TResponse>(Uri url, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default)
            => Send<TResponse>(() => new HttpRequestMessage(HttpMethod.Get, url), languageCode, cancellationToken);


        public Task<Result<TResponse, ProblemDetails>> Post<TRequest, TResponse>(Uri url, TRequest requestContent, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default)
            => Send<TResponse>(() => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = BuildContent(requestContent)
            }, languageCode, cancellationToken);


        public Task<Result<TResponse, ProblemDetails>> Post<TResponse>(Uri url, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default)
            => Send<TResponse>(() => new HttpRequestMessage(HttpMethod.Post, url), languageCode, cancellationToken);


        public Task<Result<Unit, ProblemDetails>> Post(Uri uri, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default)
            => Post<Unit, Unit>(uri, Unit.Instance, languageCode, cancellationToken);


        public Task<Result<TResponse, ProblemDetails>> Post<TResponse>(Uri url, Stream stream, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default)
            => Send<TResponse>(() => new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StreamContent(stream)
            }, languageCode, cancellationToken);


        private static StringContent BuildContent<T>(T requestContent)
            => requestContent is Unit
                ? null
                : new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");


        public async Task<Result<TResponse, ProblemDetails>> Send<TResponse>(Func<HttpRequestMessage> requestFactory, string languageCode,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await ExecuteWithRetryOnUnauthorized(requestFactory, languageCode, cancellationToken);

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var streamReader = new StreamReader(stream);
                using var jsonTextReader = new JsonTextReader(streamReader);

                if (!response.IsSuccessStatusCode)
                {
                    var error = _serializer.Deserialize<ProblemDetails>(jsonTextReader) ??
                        ProblemDetailsBuilder.Build(response.ReasonPhrase, response.StatusCode);

                    return Result.Failure<TResponse, ProblemDetails>(error);
                }

                return _serializer.Deserialize<TResponse>(jsonTextReader);
            }
            catch (Exception ex)
            {
                ex.Data.Add("requested url", requestFactory().RequestUri);
                ex.Data.Add("response body", await requestFactory().Content?.ReadAsStringAsync(cancellationToken));
                _logger.LogConnectorClientException(ex);
                return ProblemDetailsBuilder.Fail<TResponse>(ex.Message);
            }
        }


        /// <summary>
        /// Method covers two situations:
        /// 1. EDO has invalid token that is expired or belongs to old instance of Identity server (Identity and connectors were restarted but EDO did not)
        /// 2. EDO has valid token and connectors have invalid information about an Identity server certificate (Identity and EDO were restarted but connector did not)
        /// </summary>
        private async Task<HttpResponseMessage> ExecuteWithRetryOnUnauthorized(Func<HttpRequestMessage> requestFactory, string languageCode, CancellationToken cancellationToken)
        {
            using var connectorClient = _clientFactory.CreateClient(HttpClientNames.Connectors);
            connectorClient.DefaultRequestHeaders.Add("Accept-Language", languageCode);
            
            // Passing values throw context to avoid additional closure allocations.
            var policyContext = new Context(nameof(Send), new Dictionary<string, object>
            {
                { nameof(_securityTokenManager.Get), new Func<Task<string>>(_securityTokenManager.Get)},
                { nameof(requestFactory), requestFactory },
                { nameof(connectorClient), connectorClient }
            });

            return await GetUnauthorizedRetryPolicy().ExecuteAsync
            (
                action: ExecuteAuthorizedRequest,
                context: policyContext,
                cancellationToken: cancellationToken
            );
                
            
            IAsyncPolicy<HttpResponseMessage> GetUnauthorizedRetryPolicy()
            {
                // If unauthorized response returned:
                // 1. Refreshing token to make sure that it is valid
                // 2. Trying to send request one more time
                return Policy
                    .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
                    .FallbackAsync
                    (
                        onFallbackAsync: WaitAndRefreshToken,
                        fallbackAction: ExecuteAuthorizedRequest
                    );
            }
            
            
            static async Task<HttpResponseMessage> ExecuteAuthorizedRequest (Context context, CancellationToken cancelToken) 
            {
                var getTokenFunc = (Func<Task<string>>) context[nameof(_securityTokenManager.Get)];
                var createRequestFunc = (Func<HttpRequestMessage>) context[nameof(requestFactory)];
                var client = (HttpClient) context[nameof(connectorClient)];

                var token = await getTokenFunc();
                var requestMessage = createRequestFunc();
                requestMessage.SetBearerToken(token);

                return await client.SendAsync(requestMessage, cancelToken);
            }


            async Task WaitAndRefreshToken(DelegateResult<HttpResponseMessage> result, Context context)
            {
                const int delayNextRequestMilliseconds = 150;
                _logger.LogUnauthorizedConnectorResponse(result.Result?.RequestMessage?.RequestUri?.ToString() ?? string.Empty);
                await Task.Delay(TimeSpan.FromMilliseconds(delayNextRequestMilliseconds), cancellationToken);
                await _securityTokenManager.Refresh();
            }
        }
       
        
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConnectorSecurityTokenManager _securityTokenManager;
        private readonly JsonSerializer _serializer;
        private readonly ILogger<ConnectorClient> _logger;
    }
}