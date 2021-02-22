using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IAgentMarkupPolicyManager
    {
        Task<Result> Add(int agentId, MarkupPolicySettings policyData, AgentContext agent);

        Task<Result> Remove(int agentId, int policyId, AgentContext agent);

        Task<Result> Modify(int agentId, int policyId, MarkupPolicySettings settings, AgentContext agent);

        Task<List<MarkupInfo>> Get(int agentId, int agencyId);
    }
}