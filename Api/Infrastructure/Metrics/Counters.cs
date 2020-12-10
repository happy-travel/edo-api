using Prometheus;

namespace HappyTravel.Edo.Api.Infrastructure.Metrics
{
    public static class Counters
    {
        public static readonly Counter AccommodationAvailabilitySearchTimes = Prometheus.Metrics.CreateCounter(
            ApplicationPrefix + "accommodation_availability_search_total",
            "Counts start of an accommodation availability search",
            new CounterConfiguration
            {
                LabelNames = new[] {"method", "endpoint"},
            });


        public static readonly Counter WideAccommodationAvailabilitySearchTimes = Prometheus.Metrics.CreateCounter(
            ApplicationPrefix +"wide_accommodation_availability_search_total",
            "Counts start of a wide accommodation availability search",
            new CounterConfiguration
            {
                LabelNames = new[] {"method", "endpoint"},
            });
        
        private const string ApplicationPrefix = "edo_";
    }
}