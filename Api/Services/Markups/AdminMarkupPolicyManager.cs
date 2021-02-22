using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Markups.AuditEvents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class AdminMarkupPolicyManager : IAdminMarkupPolicyManager
    {
        public AdminMarkupPolicyManager(EdoContext context,
            IMarkupPolicyTemplateService templateService,
            IDateTimeProvider dateTimeProvider,
            IDisplayedMarkupFormulaService displayedMarkupFormulaService,
            IAdministratorContext administratorContext,
            IMarkupPolicyAuditService markupPolicyAuditService)
        {
            _context = context;
            _templateService = templateService;
            _dateTimeProvider = dateTimeProvider;;
            _displayedMarkupFormulaService = displayedMarkupFormulaService;
            _administratorContext = administratorContext;
            _markupPolicyAuditService = markupPolicyAuditService;
        }
        
        
        public async Task<Result> Add(MarkupPolicyData policyData)
        {
            var (_, isFailure, markupPolicy, error) = await ValidatePolicy(policyData)
                .Map(SavePolicy)
                .Tap(p => WriteAuditLog(p, MarkupPolicyEventType.AgentMarkupCreated));

            if (isFailure)
                return Result.Failure(error);

            return IsUpdateUpdateDisplayedMarkupFormulaNeeded(markupPolicy)
                ? await UpdateDisplayedMarkupFormula(markupPolicy)
                : Result.Success();


            async Task<MarkupPolicy> SavePolicy()
            {
                var now = _dateTimeProvider.UtcNow();
                var (type, counterpartyId, agencyId, agentId) = policyData.Scope;

                var policy = new MarkupPolicy
                {
                    Description = policyData.Settings.Description,
                    Order = policyData.Settings.Order,
                    ScopeType = type,
                    Target = policyData.Target,
                    AgencyId = agencyId,
                    CounterpartyId = counterpartyId,
                    AgentId = agentId,
                    TemplateSettings = policyData.Settings.TemplateSettings,
                    Currency = policyData.Settings.Currency,
                    Created = now,
                    Modified = now,
                    TemplateId = policyData.Settings.TemplateId
                };

                _context.MarkupPolicies.Add(policy);
                await _context.SaveChangesAsync();
                return policy;
            }
        }


        public async Task<Result> Remove(int policyId)
        {
            var (_, isFailure, markupPolicy, error) = await GetPolicy()
                .Map(DeletePolicy)
                .Tap(p => WriteAuditLog(p, MarkupPolicyEventType.AgentMarkupDeleted));

            if (isFailure)
                return Result.Failure(error);

            return IsUpdateUpdateDisplayedMarkupFormulaNeeded(markupPolicy)
                ? await UpdateDisplayedMarkupFormula(markupPolicy)
                : Result.Success();


            async Task<Result<MarkupPolicy>> GetPolicy()
            {
                var policy = await _context.MarkupPolicies.SingleOrDefaultAsync(p => p.Id == policyId);
                return policy == null
                    ? Result.Failure<MarkupPolicy>("Could not find policy")
                    : Result.Success(policy);
            }


            async Task<MarkupPolicy> DeletePolicy(MarkupPolicy policy)
            {
                _context.Remove(policy);
                await _context.SaveChangesAsync();
                return policy;
            }
        }


        public async Task<Result> Modify(int policyId, MarkupPolicySettings settings)
        {
            var policy = await _context.MarkupPolicies.SingleOrDefaultAsync(p => p.Id == policyId);
            if (policy == null)
                return Result.Failure("Could not find policy");

            var (_, isFailure, markupPolicy, error) = await ValidateSettings()
                .Bind(UpdatePolicy)
                .Tap(p => WriteAuditLog(p, MarkupPolicyEventType.AgentMarkupUpdated));

            if (isFailure)
                return Result.Failure(error);

            return IsUpdateUpdateDisplayedMarkupFormulaNeeded(markupPolicy)
                ? await UpdateDisplayedMarkupFormula(markupPolicy)
                : Result.Success();


            Result ValidateSettings() => _templateService.Validate(settings.TemplateId, settings.TemplateSettings);


            async Task<Result<MarkupPolicy>> UpdatePolicy()
            {
                policy.Description = settings.Description;
                policy.Order = settings.Order;
                policy.TemplateId = settings.TemplateId;
                policy.TemplateSettings = settings.TemplateSettings;
                policy.Currency = settings.Currency;
                policy.Modified = _dateTimeProvider.UtcNow();

                var (_, isFailure, error) = await ValidatePolicy(GetPolicyData(policy));
                if (isFailure)
                    return Result.Failure<MarkupPolicy>(error);

                _context.Update(policy);
                await _context.SaveChangesAsync();
                return policy;
            }
        }


        public async Task<List<MarkupPolicyData>> Get(MarkupPolicyScope scope)
        {
            return (await GetPoliciesForScope(scope))
                .Select(GetPolicyData)
                .ToList();
        }


        private Task<List<MarkupPolicy>> GetPoliciesForScope(MarkupPolicyScope scope)
        {
            var (type, counterpartyId, agencyId, agentId) = scope;
            return type switch
            {
                MarkupPolicyScopeType.Global => _context.MarkupPolicies.Where(p => p.ScopeType == MarkupPolicyScopeType.Global).ToListAsync(),
                MarkupPolicyScopeType.Counterparty => _context.MarkupPolicies
                    .Where(p => p.ScopeType == MarkupPolicyScopeType.Counterparty && p.CounterpartyId == counterpartyId)
                    .ToListAsync(),
                MarkupPolicyScopeType.Agency => _context.MarkupPolicies.Where(p => p.ScopeType == MarkupPolicyScopeType.Agency && p.AgencyId == agencyId)
                    .ToListAsync(),
                MarkupPolicyScopeType.Agent => _context.MarkupPolicies
                    .Where(p => p.ScopeType == MarkupPolicyScopeType.Agent && p.AgencyId == agencyId && p.AgentId == agentId)
                    .ToListAsync(),
                _ => Task.FromResult(new List<MarkupPolicy>(0))
            };
        }


        private static MarkupPolicyData GetPolicyData(MarkupPolicy policy)
        {
            return new MarkupPolicyData(policy.Target,
                new MarkupPolicySettings(policy.Description, policy.TemplateId, policy.TemplateSettings, policy.Order, policy.Currency),
                new MarkupPolicyScope(policy.ScopeType, policy.CounterpartyId, policy.AgencyId, policy.AgentId));
        }


        private Task<Result> ValidatePolicy(MarkupPolicyData policyData)
        {
            return ValidateTemplate()
                .Ensure(ScopeIsValid, "Invalid scope data")
                .Ensure(TargetIsValid, "Invalid policy target")
                .Ensure(PolicyOrderIsUniqueForScope, "Policy with same order is already defined");


            Result ValidateTemplate() => _templateService.Validate(policyData.Settings.TemplateId, policyData.Settings.TemplateSettings);


            bool ScopeIsValid() => policyData.Scope.Validate().IsSuccess;


            bool TargetIsValid() => policyData.Target != MarkupPolicyTarget.NotSpecified;


            async Task<bool> PolicyOrderIsUniqueForScope()
            {
                var isSameOrderPolicyExist = (await GetPoliciesForScope(policyData.Scope))
                    .Any(p => p.Order == policyData.Settings.Order);

                return !isSameOrderPolicyExist;
            }
        }
        
        
        private Task<Result> UpdateDisplayedMarkupFormula(MarkupPolicy policy)
        {
            return policy.AgentId.HasValue
                ? _displayedMarkupFormulaService.Update(policy.AgentId.Value, policy.AgencyId.Value)
                : Task.FromResult(Result.Success());
        }


        private static bool IsUpdateUpdateDisplayedMarkupFormulaNeeded(MarkupPolicy policy)
            => policy.ScopeType == MarkupPolicyScopeType.Agent;


        private async Task WriteAuditLog(MarkupPolicy policy, MarkupPolicyEventType markupPolicyEventType)
        {
            // currently logging only for agents
            if (policy.ScopeType != MarkupPolicyScopeType.Agent)
                return;

            var (_, _, administrator, _) = await _administratorContext.GetCurrent();

            await _markupPolicyAuditService.Write(markupPolicyEventType,
                new AgentMarkupPolicyData(policy.Id, policy.AgentId.Value, policy.AgencyId.Value),
                administrator.ToUserInfo());
        }


        private readonly IMarkupPolicyTemplateService _templateService;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IDisplayedMarkupFormulaService _displayedMarkupFormulaService;
        private readonly IAdministratorContext _administratorContext;
        private readonly IMarkupPolicyAuditService _markupPolicyAuditService;
    }
}