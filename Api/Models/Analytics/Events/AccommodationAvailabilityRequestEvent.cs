using System;

namespace HappyTravel.Edo.Api.Models.Analytics.Events
{
    public readonly struct AccommodationAvailabilityRequestEvent
    {
        public AccommodationAvailabilityRequestEvent(string id, string name, string counterpartyName)
        {
            Id = id;
            Name = name;
            CounterpartyName = counterpartyName;
            DateTime = DateTimeOffset.UtcNow;
        }


        public string Id { get; }
        public string CounterpartyName { get; }
        public string Name { get; }
        public DateTimeOffset DateTime { get; }
    }
}
