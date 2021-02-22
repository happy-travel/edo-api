using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Agents
{
    public class AgentAgencyRelation
    {
        public int AgentId { get; set; }
        public InAgencyPermissions InAgencyPermissions { get; set; }
        public int AgencyId { get; set; }
        public AgentAgencyRelationTypes Type { get; set; }
        public string DisplayedMarkupFormula { get; set; }
        public bool IsActive { get; set; }


        public override bool Equals(object obj) => obj is AgentAgencyRelation other && Equals(other);


        public bool Equals(AgentAgencyRelation other)
            => Equals((AgentId, InAgencyPermissions, AgencyId, Type),
                (other.AgentId, other.InAgencyPermissions, other.AgencyId, other.Type));


        public override int GetHashCode() => (AgentId, InAgencyPermissions, AgencyId, Type).GetHashCode();
    }
}