using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Models.Management.AuditEvents
{
    public readonly struct AdministratorRoleAssignmentEventData
    {
        public AdministratorRoleAssignmentEventData(int initiatorAdministratorId, int assigneeAdministratorId, List<int> newRoleIds)
        {
            InitiatorAdministratorId = initiatorAdministratorId;
            AssigneeAdministratorId = assigneeAdministratorId;
            NewRoleIds = newRoleIds;
        }


        public int InitiatorAdministratorId { get; }
        public int AssigneeAdministratorId { get; }
        public List<int> NewRoleIds { get; }
    }
}
