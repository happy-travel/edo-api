using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data.Markup;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IMarkupPolicyService
    {
        Task<List<MarkupPolicy>> Get(AgentContext agentContext, MarkupPolicyTarget policyTarget);
    }
}