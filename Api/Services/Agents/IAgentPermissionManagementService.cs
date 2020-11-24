using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public interface IAgentPermissionManagementService
    {
        Task<Result<List<InAgencyPermissions>>> SetInAgencyPermissions(int agentId, List<InAgencyPermissions> permissions, AgentContext agent);
    }
}