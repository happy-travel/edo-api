using System;
using Elasticsearch.Net;
using HappyTravel.Edo.Api.Models.Analytics.Events;

namespace HappyTravel.Edo.Api.Infrastructure.Analytics
{
    public static class ElasticLowLevelClientExtensions
    {
        static ElasticLowLevelClientExtensions()
        {
            AccommodationAvailabilityRequested = (client, accommodationInfo) =>
            {
                var datetime = DateTimeOffset.UtcNow;
                client.IndexAsync<BytesResponse>($"{ServicePrefix}-accommodation-availability-requested-{datetime:yyyy-MM-dd}", PostData.Serializable(accommodationInfo));
            };
        }


        public static void LogAccommodationAvailabilityRequested(this IElasticLowLevelClient client, AccommodationAvailabilityRequestEvent accommodationInfo) 
            => AccommodationAvailabilityRequested(client, accommodationInfo);


        private static readonly Action<IElasticLowLevelClient, AccommodationAvailabilityRequestEvent> AccommodationAvailabilityRequested;

        private const string ServicePrefix = "edo";
    }
}
