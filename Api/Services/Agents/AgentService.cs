using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using IdentityModel;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgentService : IAgentService
    {
        public AgentService(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task<Result<Agent>> Add(UserDescriptionInfo agentRegistration,
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


        public async Task<UserDescriptionInfo> UpdateCurrentAgent(UserDescriptionInfo newInfo, AgentContext agentContext)
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


        public  IQueryable<SlimAgentInfo> GetAgents(AgentContext agentContext)
        {
            var relations = _context.AgentAgencyRelations
                .Where(r => r.AgencyId == agentContext.AgencyId);

            var canObserveMarkups = agentContext.InAgencyPermissions.HasFlag(InAgencyPermissions.ObserveMarkup);

            return from relation in relations
                join agent in _context.Agents on relation.AgentId equals agent.Id
                join displayMarkupFormula in _context.DisplayMarkupFormulas on new
                {
                    relation.AgentId,
                    relation.AgencyId
                } equals new
                {
                    AgentId = displayMarkupFormula.AgentId.Value,
                    AgencyId = displayMarkupFormula.AgencyId.Value
                } into formulas
                from formula in formulas.DefaultIfEmpty()
                let name = $"{agent.FirstName} {agent.LastName}"
                let created = agent.Created.ToEpochTime()
                select new SlimAgentInfo
                {
                    AgentId = agent.Id,
                    Name = name,
                    Created = created,
                    IsActive = relation.IsActive,
                    MarkupSettings = canObserveMarkups && formula != null
                        ? formula.DisplayFormula
                        : string.Empty
                };
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
        
        
        private async ValueTask<Result> Validate(UserDescriptionInfo agentRegistration, string externalIdentity)
        {
            var fieldValidateResult = GenericValidator<UserDescriptionInfo>.Validate(v =>
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
    }
}