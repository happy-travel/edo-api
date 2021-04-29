namespace HappyTravel.Edo.Api.Models.Management.AuditEvents
{
    public readonly struct AgencyAccountActivityStatusChangeEventData
    {
        public AgencyAccountActivityStatusChangeEventData(int agencyId, int agencyAccountId, string reason)
        {
            AgencyId = agencyId;
            AgencyAccountId = agencyAccountId;
            Reason = reason;
        }
        

        public int AgencyId { get; }
        public int AgencyAccountId { get; }
        public string Reason { get; }
    }
}