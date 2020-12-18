using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Data.Bookings
{
    public class Deadline
    {
        // EF constructor
        private Deadline() {}

        [JsonConstructor]
        public Deadline(DateTime? date, List<CancellationPolicy> policies, List<string> remarks, bool isFinal)
        {
            Date = date;
            IsFinal = isFinal;
            Policies = policies ?? new List<CancellationPolicy>();
            Remarks = remarks ?? new List<string>(0);
        }
        
        public DateTime? Date { get; set; }
        public bool IsFinal { get; set; }
        public List<CancellationPolicy> Policies { get; set; }
        public List<string> Remarks { get; set; }
    }
}