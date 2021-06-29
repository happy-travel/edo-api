using System;
using System.Globalization;
using Elasticsearch.Net;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Geography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Infrastructure.Analytics
{
    public class ElasticAnalyticsService : IAnalyticsService
    {
        public ElasticAnalyticsService(IElasticLowLevelClient elasticClient,
            IDateTimeProvider dateTimeProvider,
            IWebHostEnvironment environment,
            ILogger<ElasticAnalyticsService> logger)
        {
            _elasticClient = elasticClient;
            _dateTimeProvider = dateTimeProvider;
            _environment = environment;
            _logger = logger;
        }


        public void LogEvent<TEventData>(in TEventData eventData, string name, in AgentContext agentContext, in GeoPoint? point = default)
        {
            var date = new DateTimeOffset(_dateTimeProvider.UtcNow(), TimeSpan.Zero);
            var environmentName = _environment.EnvironmentName.ToLowerInvariant();

            var indexName = $"{environmentName}-{ServicePrefix}-{name}-{date:yyyy-MM-dd}";
            var eventObject = new
            {
                DateTime = date,
                EventData = eventData,
                Counterparty = agentContext.CounterpartyName,
                Location = point.HasValue
                    ? GetElasticCoordinates(point.Value)
                    : null
            };

            _elasticClient.IndexAsync<BytesResponse>(indexName, PostData.Serializable(eventObject))
                .ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        _logger.LogElasticAnalyticsEventSendError(task.Exception);
                        return;
                    }

                    var response = task.Result;
                    if (!response.Success)
                        _logger.LogElasticAnalyticsEventSendError(response.OriginalException);
                });
        }


        private static float[] GetElasticCoordinates(GeoPoint point) 
            => new[] {(float) point.Longitude, (float) point.Latitude};


        private const string ServicePrefix = "edo";

        private readonly IElasticLowLevelClient _elasticClient;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ElasticAnalyticsService> _logger;
    }
}