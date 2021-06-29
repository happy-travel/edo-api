using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using IdentityModel.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Infrastructure.SupplierConnectors
{
    public class ConnectorSecurityTokenManager : IConnectorSecurityTokenManager
    {
        public ConnectorSecurityTokenManager(IHttpClientFactory clientFactory,
            IDateTimeProvider dateTimeProvider,
            IOptions<ConnectorTokenRequestOptions> tokenRequestOptions,
            ILogger<ConnectorSecurityTokenManager> logger)
        {
            _clientFactory = clientFactory;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _connectorTokenRequestOptions = tokenRequestOptions.Value;
        }


        public async Task Refresh()
        {
            try
            {
                // If someone refreshes token right now, there is no need to refresh it again.
                var isTokenRefreshAlreadyStarted = _refreshTokenSemaphore.CurrentCount == 0;
                // Anyway, will wait until other refresh finishes. This is indicated by released semaphore.
                await _refreshTokenSemaphore.WaitAsync();
                if (isTokenRefreshAlreadyStarted)
                    return;
                
                var now = _dateTimeProvider.UtcNow();
                using var client = _clientFactory.CreateClient(HttpClientNames.Identity);

                var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = _connectorTokenRequestOptions.Address,
                    Scope = _connectorTokenRequestOptions.Scope,
                    ClientId = _connectorTokenRequestOptions.ClientId,
                    ClientSecret = _connectorTokenRequestOptions.ClientSecret,
                    GrantType = _connectorTokenRequestOptions.GrantType
                });

                if (tokenResponse.IsError)
                {
                    _logger.LogGetTokenForConnectorError(tokenResponse.Error, _tokenInfo.Token, _tokenInfo.ExpiryDate);
                }
                else
                {
                    _tokenInfo = (tokenResponse.AccessToken, now.AddSeconds(tokenResponse.ExpiresIn));
                }
            }
            finally
            {
                _refreshTokenSemaphore.Release();
            }
        }


        public async Task<string> Get()
        {
            try
            {
                await _getTokenSemaphore.WaitAsync();
                var now = _dateTimeProvider.UtcNow();
                // Refreshing token if it's empty or will expire soon.
                if (_tokenInfo.Equals(default) || _tokenInfo.ExpiryDate <= now)
                    await Refresh();

                return _tokenInfo.Token;
            }
            finally
            {
                _getTokenSemaphore.Release();
            }
        }
        
        
        private readonly SemaphoreSlim _getTokenSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _refreshTokenSemaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<ConnectorSecurityTokenManager> _logger;
        private (string Token, DateTime ExpiryDate) _tokenInfo;
        private readonly ConnectorTokenRequestOptions _connectorTokenRequestOptions;
    }
}