using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IDisplayedMarkupFormulaService
    {
        public Task<Result> Update(int agentId, int agencyId);
    }
}