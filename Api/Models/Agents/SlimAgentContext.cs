namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct SlimAgentContext
    {
        public SlimAgentContext(int agentId, int agencyId)
        {
            AgentId = agentId;
            AgencyId = agencyId;
        }


        public int AgentId { get; }
        public int AgencyId { get; }
    }
}
