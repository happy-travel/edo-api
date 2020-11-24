using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;

namespace HappyTravel.Edo.Api.Services.Files
{
    public interface IContractFileService
    {
        Task<Result<(Stream stream, string contentType)>> Get(AgentContext agentContext);
    }
}