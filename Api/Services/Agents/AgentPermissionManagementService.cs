using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgentPermissionManagementService : IAgentPermissionManagementService
    {
        public AgentPermissionManagementService(EdoContext context, IAgentContextService agentContextService)
        {
            _context = context;
            _agentContextService = agentContextService;
        }


        public Task<Result<List<InAgencyPermissions>>> SetInAgencyPermissions(int agencyId, int agentId,
            List<InAgencyPermissions> permissionsList) =>
            SetInAgencyPermissions(agencyId, agentId, permissionsList.Aggregate((p1, p2) => p1 | p2));


        public async Task<Result<List<InAgencyPermissions>>> SetInAgencyPermissions(int agencyId, int agentId,
            InAgencyPermissions permissions)
        {
            var agent = await _agentContextService.GetAgent();

            return await Result.Success()
                .Ensure(() => agent.IsUsingAgency(agencyId), "You can only edit permissions of agents from your current agency")
                .Bind(GetRelation)
                .Ensure(IsPermissionManagementRightNotLost, "Cannot revoke last permission management rights")
                .Map(UpdatePermissions);

            async Task<Result<AgentAgencyRelation>> GetRelation()
            {
                var relation = await _context.AgentAgencyRelations
                    .SingleOrDefaultAsync(r => r.AgentId == agentId && r.AgencyId == agencyId);

                return relation is null
                    ? Result.Failure<AgentAgencyRelation>(
                        $"Could not find relation between the agent {agentId} and the agency {agencyId}")
                    : Result.Success(relation);
            }


            async Task<bool> IsPermissionManagementRightNotLost(AgentAgencyRelation relation)
            {
                if (permissions.HasFlag(InAgencyPermissions.PermissionManagement))
                    return true;

                return (await _context.AgentAgencyRelations
                        .Where(r => r.AgencyId == relation.AgencyId && r.AgentId != relation.AgentId)
                        .ToListAsync())
                    .Any(c => c.InAgencyPermissions.HasFlag(InAgencyPermissions.PermissionManagement));
            }


            async Task<List<InAgencyPermissions>> UpdatePermissions(AgentAgencyRelation relation)
            {
                relation.InAgencyPermissions = permissions;

                _context.AgentAgencyRelations.Update(relation);
                await _context.SaveChangesAsync();

                return relation.InAgencyPermissions.ToList();
            }
        }


        private readonly EdoContext _context;
        private readonly IAgentContextService _agentContextService;
    }
}