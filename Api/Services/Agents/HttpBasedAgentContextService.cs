using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class HttpBasedAgentContextService : IAgentContextService, IAgentContextInternal
    {
        public HttpBasedAgentContextService(EdoContext context,
            ITokenInfoAccessor tokenInfoAccessor,
            IDoubleFlow flow)
        {
            _context = context;
            _tokenInfoAccessor = tokenInfoAccessor;
            _flow = flow;
        }


        public async ValueTask<Result<AgentContext>> GetAgentInfo()
        {
            if (!_currentAgentContext.Equals(default))
                return Result.Success(_currentAgentContext);

            var identityHash = GetUserIdentityHash();
            var key = _flow.BuildKey(nameof(HttpBasedAgentContextService), nameof(GetAgentInfo), identityHash);
            
            _currentAgentContext = await _flow.GetOrSetAsync(
                key: key,
                getValueFunction: async () => await GetAgentInfoByIdentityHash(identityHash),
                AgentContextCacheLifeTime);

            return _currentAgentContext.Equals(default)
                ? Result.Failure<AgentContext>("Could not get agent data")
                : Result.Success(_currentAgentContext);
        }


        public async ValueTask<AgentContext> GetAgent()
        {
            var (_, isFailure, agent, error) = await GetAgentInfo();
            // Normally this should not happen and such error is a signal that something is going wrong.
            if (isFailure)
                throw new UnauthorizedAccessException("Agent retrieval failure");

            return agent;
        }


        private async ValueTask<AgentContext> GetAgentInfoByIdentityHash(string identityHash)
        {
            // TODO: use counterparty information from headers to get counterparty id
            // TODO: this method assumes that only one relation exists for given AgentId, which is now not true. Needs rework. NIJO-623.
            return await (from agent in _context.Agents
                    from agentAgencyRelation in _context.AgentAgencyRelations.Where(r => r.AgentId == agent.Id)
                    from agency in _context.Agencies.Where(a => a.Id == agentAgencyRelation.AgencyId && a.IsActive)
                    from counterparty in _context.Counterparties.Where(c => c.Id == agency.CounterpartyId && c.IsActive)
                    where agent.IsActive && agentAgencyRelation.IsActive && agent.IdentityHash == identityHash
                    select new AgentContext(agent.Id,
                        agent.FirstName,
                        agent.LastName,
                        agent.Email,
                        agent.Title,
                        agent.Position,
                        counterparty.Id,
                        counterparty.Name,
                        agency.Id,
                        agentAgencyRelation.Type == AgentAgencyRelationTypes.Master,
                        agentAgencyRelation.InAgencyPermissions))
                .SingleOrDefaultAsync();
        }


        private string GetUserIdentityHash()
        {
            var identityClaim = _tokenInfoAccessor.GetIdentity();
            return identityClaim != null
                ? HashGenerator.ComputeSha256(identityClaim)
                : string.Empty;
        }

        private static readonly TimeSpan AgentContextCacheLifeTime = TimeSpan.FromMinutes(2);
        
        private readonly EdoContext _context;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
        private readonly IDoubleFlow _flow;
        private AgentContext _currentAgentContext;
    }
}