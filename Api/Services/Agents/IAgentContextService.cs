using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Agents;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public interface IAgentContextService
    {
        ValueTask<AgentContext> GetAgent();

        ValueTask<AgentContext> GetAgent(int agentId);
    }
}