using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class DisplayedMarkupFormulaService : IDisplayedMarkupFormulaService
    {
        public DisplayedMarkupFormulaService(EdoContext context, IMarkupPolicyTemplateService markupPolicyTemplateService)
        {
            _context = context;
            _markupPolicyTemplateService = markupPolicyTemplateService;
        }


        public async Task<Result> Update(int agentId, int agencyId)
        {
            var relation = await _context.AgentAgencyRelations
                .SingleOrDefaultAsync(r => r.AgentId == agentId && r.AgencyId == agencyId);

            if (relation is null)
                return Result.Failure<Agent>($"Agent with id {agentId} not found in agency with id {agencyId}");

            relation.DisplayedMarkupFormula = await GetAgentMarkupFormula(relation.AgentId, relation.AgencyId);
            _context.AgentAgencyRelations.Update(relation);
            await _context.SaveChangesAsync();

            return Result.Success();
        }


        private async Task<string> GetAgentMarkupFormula(int agentId, int agencyId)
        {
            var policies = await _context.MarkupPolicies
                .Where(p => p.AgentId == agentId && p.AgencyId == agencyId && p.ScopeType == MarkupPolicyScopeType.Agent)
                .OrderBy(p => p.Order)
                .ToListAsync();

            return policies.Any()
                ? _markupPolicyTemplateService.GetMarkupsFormula(policies)
                : string.Empty;
        }


        private readonly EdoContext _context;
        private readonly IMarkupPolicyTemplateService _markupPolicyTemplateService;
    }
}