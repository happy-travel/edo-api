namespace HappyTravel.Edo.Api.Models.Management.AuditEvents
{
    public readonly struct CounterpartyAccountActivityStatusChangeEventData
    {
        public CounterpartyAccountActivityStatusChangeEventData(int counterpartyId, int counterpartyAccountId, string reason)
        {
            CounterpartyId = counterpartyId;
            CounterpartyAccountId = counterpartyAccountId;
            Reason = reason;
        }
        

        public int CounterpartyId { get; }
        public int CounterpartyAccountId { get; }
        public string Reason { get; }
    }
}