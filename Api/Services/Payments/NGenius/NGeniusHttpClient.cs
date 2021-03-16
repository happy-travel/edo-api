using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public class NGeniusHttpClient
    {
        public NGeniusHttpClient(IOptions<NGeniusOptions> options, 
            HttpClient client,
            IMemoryFlow<string> cache)
        {
            _options = options.Value;
            _client = client;
            _cache = cache;
        }


        private async Task<string> GetAccessToken()
        {
            var key = _cache.BuildKey(nameof(NGeniusHttpClient), "access-token");
            
            if (!_cache.TryGetValue<string>(key, out var token))
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _options.Token);
                var response = await _client.SendAsync(request);
                
                response.EnsureSuccessStatusCode();

                var data = JsonSerializer.Deserialize<AuthResponse>(await response.Content.ReadAsStringAsync());
                _cache.Set(key, data.AccessToken, TimeSpan.FromSeconds(data.ExpiresIn));
            }

            return token;
        }


        public async Task<Result<Guid>> CreateOrder(OrderRequest order)
        {
            var endpoint = $"{_options.Endpoint}/{_options.OutletId}/orders";
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json")
            };
            var token = await GetAccessToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(json);
            var paymentId =  document.RootElement
                .GetProperty("_embedded")
                .GetProperty("payments")
                .EnumerateArray()
                .Take(1)
                .FirstOrDefault()
                .GetProperty("_id")
                .GetString()
                ?.Split(":")
                .LastOrDefault();

            if (string.IsNullOrEmpty(paymentId) || Guid.TryParse(paymentId, out var p))
                return Result.Failure<Guid>("Failed to get payment id");

            return p;
        }


        public async Task<Result<string>> AddPaymentInformation(string referenceCode, Guid paymentId, PaymentInformation paymentInformation)
        {
            var endpoint = $"{_options.Endpoint}/{_options.OutletId}/orders/{referenceCode}/{paymentId}/card";
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(paymentInformation), Encoding.UTF8, "application/json")
            };
            var token = await GetAccessToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(json);
            return document.RootElement
                .GetProperty("state")
                .GetString();
        }


        public async Task<Guid> Capture(Guid paymentId, string referenceCode, Amount amount)
        {
            var endpoint = $"{_options.Endpoint}/{_options.OutletId}/orders/{referenceCode}/{paymentId}/captures";
            var token = await GetAccessToken();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(amount), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            // TODO: get guid from response
            return Guid.NewGuid();
        }
        
        
        public async Task<Result> Void(Guid paymentId, string referenceCode)
        {
            var endpoint = $"{_options.Endpoint}/{_options.OutletId}/orders/{referenceCode}/{paymentId}/cancel";
            var token = await GetAccessToken();
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            
            response.EnsureSuccessStatusCode();

            return Result.Success();
        }
        
        
        public async Task<Result> Refund(Guid paymentId, Guid captureId, string referenceCode)
        {
            var endpoint = $"{_options.Endpoint}/{_options.OutletId}/orders/{referenceCode}/{paymentId}/captures/{captureId}/refund";
            var token = await GetAccessToken();
            var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            
            response.EnsureSuccessStatusCode();
            
            return Result.Success();
        }


        private readonly NGeniusOptions _options;
        private readonly HttpClient _client;
        private readonly IMemoryFlow<string> _cache;
    }
}