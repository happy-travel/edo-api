using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public interface IAgentContextInternal
    {
        ValueTask<Result<AgentContext>> GetAgentInfo();
        
        Task RefreshAgentContext();
    }
}