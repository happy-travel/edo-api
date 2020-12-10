using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupPolicyService : IMarkupPolicyService
    {
        public MarkupPolicyService(EdoContext context, 
            IDoubleFlow flow,
            IAgentSettingsManager agentSettingsManager,
            IAccommodationBookingSettingsService accommodationBookingSettingsService)
        {
            _context = context;
            _flow = flow;
            _agentSettingsManager = agentSettingsManager;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
        }


        public async Task<List<MarkupPolicy>> Get(AgentContext agentContext, MarkupPolicyTarget policyTarget)
        {
            var searchSettings = await _accommodationBookingSettingsService.Get(agentContext);
            if (searchSettings.IsMarkupDisabled)
                return new List<MarkupPolicy>(0);
            
            var agentSettings = await GetAgentSettings(agentContext);
            return await GetAgentPolicies(agentContext, agentSettings, policyTarget);
        }


        private Task<AgentUserSettings> GetAgentSettings(AgentContext agentContext)
        {
            return _flow.GetOrSetAsync(
                key: BuildKey(),
                getValueFunction: async () => await _agentSettingsManager.GetUserSettings(agentContext),
                AgentSettingsCachingTime); 
            
            string BuildKey()
                => _flow.BuildKey(nameof(MarkupPolicyService),
                    nameof(GetAgentSettings),
                    agentContext.AgentId.ToString());
        }


        private Task<List<MarkupPolicy>> GetAgentPolicies(AgentContext agentContext, AgentUserSettings userSettings,
            MarkupPolicyTarget policyTarget)
        {
            var (agentId, counterpartyId, agencyId, _) = agentContext;

            return _flow.GetOrSetAsync(BuildKey(),
                GetOrderedPolicies,
                AgentPoliciesCachingTime);


            string BuildKey()
                => _flow.BuildKey(nameof(MarkupPolicyService),
                    nameof(GetAgentPolicies),
                    agentId.ToString());


            Task<List<MarkupPolicy>> GetOrderedPolicies() 
                => _context.MarkupPolicies
                .Where(p => p.Target == policyTarget)
                .Where(p =>
                    p.ScopeType == MarkupPolicyScopeType.Global ||
                    p.ScopeType == MarkupPolicyScopeType.Counterparty && p.CounterpartyId == counterpartyId ||
                    p.ScopeType == MarkupPolicyScopeType.Agency && p.AgencyId == agencyId ||
                    p.ScopeType == MarkupPolicyScopeType.Agent && p.AgentId == agentId ||
                    p.ScopeType == MarkupPolicyScopeType.EndClient && p.AgentId == agentId
                )
                .Where(p => p.ScopeType != MarkupPolicyScopeType.EndClient || userSettings.IsEndClientMarkupsEnabled)
                .OrderBy(SortByScope)
                .ThenBy(p => p.Order)
                .ToListAsync();
        }

        private static readonly Expression<Func<MarkupPolicy, int>> SortByScope = policy => (int) policy.ScopeType;

        private static readonly TimeSpan AgentPoliciesCachingTime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan AgentSettingsCachingTime = TimeSpan.FromMinutes(2);
        private readonly EdoContext _context;
       
        private readonly IAgentSettingsManager _agentSettingsManager;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IDoubleFlow _flow;
    }
}