using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Api.Services.PriceProcessing;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupService : IMarkupService
    {
        public MarkupService(EdoContext context, 
            IDoubleFlow flow,
            IMarkupPolicyTemplateService templateService,
            ICurrencyRateService currencyRateService,
            IAgentSettingsManager agentSettingsManager,
            IAgencySystemSettingsService agencySystemSettingsService)
        {
            _context = context;
            _flow = flow;
            _templateService = templateService;
            _currencyRateService = currencyRateService;
            _agentSettingsManager = agentSettingsManager;
            _agencySystemSettingsService = agencySystemSettingsService;
        }


        public async Task<Markup> Get(AgentContext agentContext, MarkupPolicyTarget policyTarget)
        {
            var agencySettings = await GetAvailabilitySearchSettings(agentContext);
            if (agencySettings.HasValue && agencySettings.Value.IsMarkupDisabled)
                return Markup.Empty;
            
            var agentSettings = await GetAgentSettings(agentContext);
            var agentPolicies = await GetAgentPolicies(agentContext, agentSettings, policyTarget);
            var markupFunction = CreateAggregatedMarkupFunction(agentPolicies);
            return new Markup
            {
                Policies = agentPolicies,
                Function = markupFunction
            };
        }


        private Task<Maybe<AgencyAvailabilitySearchSettings>> GetAvailabilitySearchSettings(AgentContext agentContext)
        {
            return _flow.GetOrSetAsync(
                key: BuildKey(),
                getValueFunction: async () => await _agencySystemSettingsService.GetAvailabilitySearchSettings(agentContext.AgencyId),
                AgencySettingsCachingTime); 
            
            string BuildKey()
                => _flow.BuildKey(nameof(MarkupService),
                    nameof(GetAvailabilitySearchSettings),
                    agentContext.AgencyId.ToString());
        }


        private Task<AgentUserSettings> GetAgentSettings(AgentContext agentContext)
        {
            return _flow.GetOrSetAsync(
                key: BuildKey(),
                getValueFunction: async () => await _agentSettingsManager.GetUserSettings(agentContext),
                AgentSettingsCachingTime); 
            
            string BuildKey()
                => _flow.BuildKey(nameof(MarkupService),
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
                => _flow.BuildKey(nameof(MarkupService),
                    nameof(GetAgentPolicies),
                    agentId.ToString());


            async Task<List<MarkupPolicy>> GetOrderedPolicies()
            {
                var policiesFromDb = await GetPoliciesFromDb();
                return policiesFromDb.Where(FilterBySettings)
                    .OrderBy(SortByScope)
                    .ThenBy(p => p.Order)
                    .ToList();
            }


            bool FilterBySettings(MarkupPolicy policy)
            {
                if (policy.ScopeType == MarkupPolicyScopeType.EndClient && !userSettings.IsEndClientMarkupsEnabled)
                    return false;

                return true;
            }


            int SortByScope(MarkupPolicy policy) => (int) policy.ScopeType;


            Task<List<MarkupPolicy>> GetPoliciesFromDb()
            {
                return _context.MarkupPolicies
                    .Where(p => p.Target == policyTarget)
                    .Where(p =>
                        p.ScopeType == MarkupPolicyScopeType.Global ||
                        p.ScopeType == MarkupPolicyScopeType.Counterparty && p.CounterpartyId == counterpartyId ||
                        p.ScopeType == MarkupPolicyScopeType.Agency && p.AgencyId == agencyId ||
                        p.ScopeType == MarkupPolicyScopeType.Agent && p.AgentId == agentId ||
                        p.ScopeType == MarkupPolicyScopeType.EndClient && p.AgentId == agentId
                    )
                    .ToListAsync();
            }
        }


        private PriceProcessFunction CreateAggregatedMarkupFunction(List<MarkupPolicy> policies)
        {
            var markupPolicyFunctions = policies
                .Select(GetPolicyFunction)
                .ToList();

            // TODO: rewrite to async streams after migrating to .NET Core 3
            return async (supplierPrice, currency) =>
            {
                var price = supplierPrice;
                foreach (var markupPolicyFunction in markupPolicyFunctions)
                {
                    var (_, _, currencyRate, _) = await _currencyRateService.Get(currency, markupPolicyFunction.Currency);
                    price = markupPolicyFunction.Function(price * currencyRate) / currencyRate;
                }

                return (price, currency);
            };
        }


        private MarkupPolicyFunction GetPolicyFunction(MarkupPolicy policy)
        {
            return _flow
                .GetOrSet(BuildKey(policy),
                    () =>
                    {
                        return new MarkupPolicyFunction
                        {
                            Currency = policy.Currency,
                            Function = _templateService
                                .CreateFunction(policy.TemplateId, policy.TemplateSettings)
                        };
                    },
                    MarkupPolicyFunctionCachingTime);


            string BuildKey(MarkupPolicy policyWithFunc)
                => _flow.BuildKey(nameof(MarkupService),
                    nameof(GetPolicyFunction),
                    policyWithFunc.Id.ToString(),
                    policyWithFunc.Modified.ToString(CultureInfo.InvariantCulture));
        }


        private static readonly TimeSpan MarkupPolicyFunctionCachingTime = TimeSpan.FromDays(1);
        private static readonly TimeSpan AgentPoliciesCachingTime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan AgentSettingsCachingTime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan AgencySettingsCachingTime = TimeSpan.FromMinutes(2);
        private readonly EdoContext _context;
        private readonly ICurrencyRateService _currencyRateService;
        private readonly IAgentSettingsManager _agentSettingsManager;
        private readonly IAgencySystemSettingsService _agencySystemSettingsService;
        private readonly IDoubleFlow _flow;
        private readonly IMarkupPolicyTemplateService _templateService;
    }
}