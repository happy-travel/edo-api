using System;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Data.Bookings
{
    public class CancellationPolicy
    {
        // EF constructor
        private CancellationPolicy() { }

        [JsonConstructor]
        public CancellationPolicy(DateTime fromDate, double percentage)
        {
            FromDate = fromDate;
            Percentage = percentage;
        }
        
        public DateTime FromDate { get; set; }
        public double Percentage { get; set; }
    }
}