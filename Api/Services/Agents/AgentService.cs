using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgentService : IAgentService
    {
        public AgentService(EdoContext context, IDateTimeProvider dateTimeProvider,
            IMarkupPolicyTemplateService markupPolicyTemplateService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _markupPolicyTemplateService = markupPolicyTemplateService;
        }


        public async Task<Result<Agent>> Add(AgentEditableInfo agentRegistration,
            string externalIdentity,
            string email)
        {
            var (_, isFailure, error) = await Validate(agentRegistration, externalIdentity);
            if (isFailure)
                return Result.Failure<Agent>(error);

            var createdAgent = new Agent
            {
                Title = agentRegistration.Title,
                FirstName = agentRegistration.FirstName,
                LastName = agentRegistration.LastName,
                Position = agentRegistration.Position,
                Email = email,
                IdentityHash = HashGenerator.ComputeSha256(externalIdentity),
                Created = _dateTimeProvider.UtcNow()
            };

            _context.Agents.Add(createdAgent);
            await _context.SaveChangesAsync();

            return Result.Success(createdAgent);
        }


        public async Task<Result<Agent>> GetMasterAgent(int agencyId)
        {
            var master = await (from a in _context.Agents
                join rel in _context.AgentAgencyRelations on a.Id equals rel.AgentId
                where rel.AgencyId == agencyId && rel.Type == AgentAgencyRelationTypes.Master
                select a).FirstOrDefaultAsync();

            if (master is null)
                return Result.Failure<Agent>("Master agent does not exist");

            return Result.Success(master);
        }


        public async Task<AgentEditableInfo> UpdateCurrentAgent(AgentEditableInfo newInfo, AgentContext agentContext)
        {
            var agentToUpdate = await _context.Agents.SingleAsync(a => a.Id == agentContext.AgentId);

            agentToUpdate.FirstName = newInfo.FirstName;
            agentToUpdate.LastName = newInfo.LastName;
            agentToUpdate.Title = newInfo.Title;
            agentToUpdate.Position = newInfo.Position;

            _context.Agents.Update(agentToUpdate);
            await _context.SaveChangesAsync();

            return newInfo;
        }

        public async Task<Result<List<SlimAgentInfo>>> GetAgents(AgentContext agentContext)
        {
            var hasObserveMarkupPermission = agentContext.InAgencyPermissions.HasFlag(InAgencyPermissions.ObserveMarkup);

            var relations = await
                (from relation in _context.AgentAgencyRelations
                    join agent in _context.Agents
                        on relation.AgentId equals agent.Id
                    where relation.AgencyId == agentContext.AgencyId
                    select new {relation, agent})
                .ToListAsync();

            var agentIdList = relations.Select(x => x.agent.Id).ToList();

            var markupsMap = (await (
                from markup in _context.MarkupPolicies
                where markup.AgentId != null 
                    && agentIdList.Contains(markup.AgentId.Value)
                    && markup.ScopeType == MarkupPolicyScopeType.Agent
                select markup)
                .ToListAsync())
                .GroupBy(k => (int)k.AgentId)
                .ToDictionary(k => k.Key, v => v.ToList());

            var results = relations.Select(o =>
                new SlimAgentInfo(o.agent.Id, o.agent.FirstName, o.agent.LastName,
                    o.agent.Created, GetMarkupFormula(o.relation), o.relation.IsActive))
                .ToList();

            return Result.Success(results);

            string GetMarkupFormula(AgentAgencyRelation relation)
            {
                if (!markupsMap.TryGetValue(relation.AgentId, out var policies))
                    return string.Empty;

                if (!hasObserveMarkupPermission)
                    return string.Empty;

                return _markupPolicyTemplateService.GetMarkupsFormula(policies);
            }
        }


        public async Task<Result<AgentInfoInAgency>> GetAgent(int agentId, AgentContext agentContext)
        {
            var foundAgent = await (
                    from cr in _context.AgentAgencyRelations
                    join agent in _context.Agents
                        on cr.AgentId equals agent.Id
                    join agency in _context.Agencies
                        on cr.AgencyId equals agency.Id
                    join counterparty in _context.Counterparties
                        on agency.CounterpartyId equals counterparty.Id
                    where cr.AgencyId == agentContext.AgencyId && cr.AgentId == agentId
                    select (AgentInfoInAgency?) new AgentInfoInAgency(agent.Id, agent.FirstName, agent.LastName, agent.Email, agent.Title, agent.Position, counterparty.Id, counterparty.Name,
                        cr.AgencyId, agency.Name, cr.Type == AgentAgencyRelationTypes.Master, cr.InAgencyPermissions.ToList(), cr.IsActive))
                .SingleOrDefaultAsync();

            if (foundAgent == null)
                return Result.Failure<AgentInfoInAgency>("Agent not found in specified agency");

            return foundAgent.Value;
        }


        public Task<List<AgentAgencyRelationInfo>> GetAgentRelations(AgentContext agent)
        {
            return (from cr in _context.AgentAgencyRelations
                join ag in _context.Agencies
                    on cr.AgencyId equals ag.Id
                join co in _context.Counterparties
                    on ag.CounterpartyId equals co.Id
                where ag.IsActive && co.IsActive && cr.AgentId == agent.AgentId
                select new AgentAgencyRelationInfo(
                    co.Id,
                    co.Name,
                    ag.Id,
                    ag.Name,
                    cr.Type == AgentAgencyRelationTypes.Master,
                    GetActualPermissions(co.State, cr.InAgencyPermissions),
                    co.State))
                .ToListAsync();
        }


        private static List<InAgencyPermissions> GetActualPermissions(CounterpartyStates counterpartyState, InAgencyPermissions inAgencyPermissions)
        {
            const InAgencyPermissions readOnlyPermissions = InAgencyPermissions.AccommodationAvailabilitySearch |
                InAgencyPermissions.AgentInvitation |
                InAgencyPermissions.PermissionManagement |
                InAgencyPermissions.ObserveAgents;
                
            switch (counterpartyState)
            {
                case CounterpartyStates.DeclinedVerification: 
                case CounterpartyStates.PendingVerification:
                    return new List<InAgencyPermissions>(0);
                case CounterpartyStates.ReadOnly:
                    return (inAgencyPermissions & readOnlyPermissions).ToList();
                case CounterpartyStates.FullAccess:
                    return inAgencyPermissions.ToList();
                default:
                    throw new ArgumentException($"Invalid counterparty state {counterpartyState}");
            }
        }
        
        
        private async ValueTask<Result> Validate(AgentEditableInfo agentRegistration, string externalIdentity)
        {
            var fieldValidateResult = GenericValidator<AgentEditableInfo>.Validate(v =>
            {
                v.RuleFor(a => a.Title).NotEmpty();
                v.RuleFor(a => a.FirstName).NotEmpty();
                v.RuleFor(a => a.LastName).NotEmpty();
            }, agentRegistration);

            if (fieldValidateResult.IsFailure)
                return fieldValidateResult;

            return await CheckIdentityIsUnique(externalIdentity);
        }


        private async Task<Result> CheckIdentityIsUnique(string identity)
        {
            return await _context.Agents.AnyAsync(a => a.IdentityHash == HashGenerator.ComputeSha256(identity))
                ? Result.Failure("User is already registered")
                : Result.Success();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMarkupPolicyTemplateService _markupPolicyTemplateService;
    }
}