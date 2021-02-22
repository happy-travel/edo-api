using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Markups.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class AgentMarkupPolicyManager : IAgentMarkupPolicyManager
    {
        public AgentMarkupPolicyManager(EdoContext context,
            IMarkupPolicyTemplateService templateService,
            IDateTimeProvider dateTimeProvider,
            IDisplayedMarkupFormulaService displayedMarkupFormulaService,
            IMarkupPolicyAuditService markupPolicyAuditService)
        {
            _context = context;
            _templateService = templateService;
            _dateTimeProvider = dateTimeProvider;
            _displayedMarkupFormulaService = displayedMarkupFormulaService;
            _markupPolicyAuditService = markupPolicyAuditService;
        }


        public Task<Result> Add(int agentId, MarkupPolicySettings settings, AgentContext agent)
        {
            return ValidateSettings(agentId, agent.AgencyId, settings)
                .Bind(() => GetAgentAgencyRelation(agentId, agent.AgencyId))
                .Map(SavePolicy)
                .Tap(WriteAuditLog)
                .Bind(UpdateDisplayedMarkupFormula);


            async Task<MarkupPolicy> SavePolicy(AgentAgencyRelation agentAgencyRelation)
            {
                var now = _dateTimeProvider.UtcNow();

                var policy = new MarkupPolicy
                {
                    Description = settings.Description,
                    Order = settings.Order,
                    ScopeType = MarkupPolicyScopeType.Agent,
                    Target = MarkupPolicyTarget.AccommodationAvailability,
                    AgencyId = agentAgencyRelation.AgencyId,
                    CounterpartyId = null,
                    AgentId = agentAgencyRelation.AgentId,
                    TemplateSettings = settings.TemplateSettings,
                    Currency = settings.Currency,
                    Created = now,
                    Modified = now,
                    TemplateId = settings.TemplateId
                };

                _context.MarkupPolicies.Add(policy);
                await _context.SaveChangesAsync();
                return policy;
            }


            Task WriteAuditLog(MarkupPolicy policy)
                => _markupPolicyAuditService.Write(MarkupPolicyEventType.AgentMarkupCreated,
                    new AgentMarkupPolicyData(policy.Id, policy.AgentId.Value, policy.AgencyId.Value),
                    agent.ToUserInfo());
        }


        public Task<Result> Remove(int agentId, int policyId, AgentContext agent)
        {
            return GetAgentAgencyRelation(agentId, agent.AgencyId) 
                .Bind(GetPolicy)
                .Map(DeletePolicy)
                .Tap(WriteAuditLog)
                .Bind(UpdateDisplayedMarkupFormula);


            Task<Result<MarkupPolicy>> GetPolicy(AgentAgencyRelation relation) => GetAgentPolicy(relation, policyId);


            async Task<MarkupPolicy> DeletePolicy(MarkupPolicy policy)
            {
                _context.Remove(policy);
                await _context.SaveChangesAsync();
                return policy;
            }


            Task WriteAuditLog(MarkupPolicy policy)
                => _markupPolicyAuditService.Write(MarkupPolicyEventType.AgentMarkupDeleted,
                    new AgentMarkupPolicyData(policy.Id, policy.AgentId.Value, policy.AgencyId.Value),
                    agent.ToUserInfo());
        }


        public async Task<Result> Modify(int agentId, int policyId, MarkupPolicySettings settings, AgentContext agent)
        {
            return await GetAgentAgencyRelation(agentId, agent.AgencyId) 
                .Bind(GetPolicy)
                .Check(_ => ValidateSettings(agentId, agent.AgencyId, settings, policyId))
                .Tap(UpdatePolicy)
                .Tap(WriteAuditLog)
                .Bind(UpdateDisplayedMarkupFormula);


            Task<Result<MarkupPolicy>> GetPolicy(AgentAgencyRelation relation) => GetAgentPolicy(relation, policyId);

            
            async Task UpdatePolicy(MarkupPolicy policy)
            {
                policy.Description = settings.Description;
                policy.Order = settings.Order;
                policy.TemplateId = settings.TemplateId;
                policy.TemplateSettings = settings.TemplateSettings;
                policy.Currency = settings.Currency;
                policy.Modified = _dateTimeProvider.UtcNow();

                _context.Update(policy);
                await _context.SaveChangesAsync();
            }


            Task WriteAuditLog(MarkupPolicy policy)
                => _markupPolicyAuditService.Write(MarkupPolicyEventType.AgentMarkupUpdated,
                    new AgentMarkupPolicyData(policy.Id, policy.AgentId.Value, policy.AgencyId.Value),
                    agent.ToUserInfo());
        }


        public async Task<List<MarkupInfo>> Get(int agentId, int agencyId)
        {
            var policies = await GetAgentPolicies(agentId, agencyId);
            return policies
                .Select(p=> new MarkupInfo(p.Id, p.GetSettings()))
                .ToList();
        }


        private Task<List<MarkupPolicy>> GetAgentPolicies(int agentId, int agencyId)
        {
            return _context.MarkupPolicies
                .Where(p => p.ScopeType == MarkupPolicyScopeType.Agent && p.AgentId == agentId && p.AgencyId == agencyId)
                .OrderBy(p => p.Order)
                .ToListAsync();
        }

        
        private async Task<Result<AgentAgencyRelation>> GetAgentAgencyRelation(int agentId, int agencyId)
        {
            var relation = await _context.AgentAgencyRelations
                .SingleOrDefaultAsync(r => r.AgentId == agentId && r.AgencyId == agencyId);
            
            return relation ?? Result.Failure<AgentAgencyRelation>("Could not find this agent in your agency"); 
        }
        

        private Task<Result> ValidateSettings(int agentId, int agencyId, MarkupPolicySettings settings, int? policyId = null)
        {
            return ValidateTemplate()
                .Ensure(PolicyOrderIsUniqueForScope, "Policy with same order is already defined");


            Result ValidateTemplate() => _templateService.Validate(settings.TemplateId, settings.TemplateSettings);


            async Task<bool> PolicyOrderIsUniqueForScope()
            {
                var isSameOrderPolicyExist = (await GetAgentPolicies(agentId, agencyId))
                    .Any(p => p.Order == settings.Order && p.Id != policyId);

                return !isSameOrderPolicyExist;
            }
        }


        private async Task<Result<MarkupPolicy>> GetAgentPolicy(AgentAgencyRelation relation, int policyId)
        {
            var policy = await _context.MarkupPolicies
                .SingleOrDefaultAsync(p => p.Id == policyId && p.ScopeType == MarkupPolicyScopeType.Agent && p.AgentId.HasValue 
                    && p.AgentId.Value == relation.AgentId && p.AgencyId.HasValue && p.AgencyId == relation.AgencyId);

            return policy ?? Result.Failure<MarkupPolicy>("Could not find agent policy");
        }


        private Task<Result> UpdateDisplayedMarkupFormula(MarkupPolicy policy)
            => _displayedMarkupFormulaService.Update(policy.AgentId.Value, policy.AgencyId.Value);


        private readonly IMarkupPolicyTemplateService _templateService;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDisplayedMarkupFormulaService _displayedMarkupFormulaService;
        private readonly IMarkupPolicyAuditService _markupPolicyAuditService;
    }
}