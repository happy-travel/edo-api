using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Services.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgentInvitationService : IAgentInvitationService
    {
        public AgentInvitationService(IOptions<AgentInvitationOptions> options,
            IUserInvitationService invitationService,
            ICounterpartyService counterpartyService,
            EdoContext context)
        {
            _invitationService = invitationService;
            _counterpartyService = counterpartyService;
            _options = options.Value;
            _context = context;
        }


        public async Task<Result> Send(SendAgentInvitationRequest request, AgentContext agent)
        {
            var agencyName = (await _counterpartyService.GetAgency(agent.AgencyId, agent)).Value.Name;

            var messagePayloadGenerator = new Func<AgentInvitationInfo, string, DataWithCompanyInfo>((info, invitationCode) => new AgentInvitationData
            {
                AgencyName = agencyName,
                InvitationCode = invitationCode,
                UserEmailAddress = info.Email,
                UserName = $"{info.RegistrationInfo.FirstName} {info.RegistrationInfo.LastName}"
            });

            return await _invitationService.Send(request.Email, request.ToAgentInvitationInfo(agent), messagePayloadGenerator,
                _options.MailTemplateId, UserInvitationTypes.Agent);
        }


        public async Task<Result<string>> Create(SendAgentInvitationRequest request, AgentContext agent)
        {
            var invitationInfo = request.ToAgentInvitationInfo(agent);
            return await _invitationService.Create(invitationInfo.Email, invitationInfo, UserInvitationTypes.Agent);
        }


        public Task Accept(string invitationCode) => _invitationService.Accept(invitationCode);


        public Task<Result<AgentInvitationInfo>> GetPendingInvitation(string invitationCode)
            => _invitationService.GetPendingInvitation<AgentInvitationInfo>(invitationCode, UserInvitationTypes.Agent);


        public Task<List<AgentInvitationResponse>> GetAgentInvitations(int agentId)
            => GetInvitations(i => i.Data.AgentId == agentId);


        public Task<List<AgentInvitationResponse>> GetAgencyInvitations(int agencyId)
            => GetInvitations(i => i.Data.AgencyId == agencyId);


        public async Task<Result> Resend(string invitationCode, AgentContext agent)
        {
            return await GetExistingInvitation()
                .Tap(SendInvitation)
                .Bind(DisableExistingInvitation);


            async Task<Result<AgentInvitation>> GetExistingInvitation()
            {
                var invitation = await _context
                    .AgentInvitations
                    .SingleOrDefaultAsync(i => i.CodeHash == invitationCode);

                return invitation ?? Result.Failure<AgentInvitation>($"Invitation with Code {invitationCode} not found");
            }


            Task<Result> SendInvitation(AgentInvitation existingInvitation)
                => Send(existingInvitation.ToSendAgentInvitationRequest(), agent);


            async Task<Result> DisableExistingInvitation(AgentInvitation existingInvitation)
            {
                if (existingInvitation is null)
                {
                    return Result.Failure("Old invitation can not be null");
                }

                existingInvitation.IsResent = true;
                await _context.SaveChangesAsync();
                return Result.Success();
            }
        }


        private Task<List<AgentInvitationResponse>> GetInvitations(Expression<Func<AgentInvitation, bool>> filterExpression)
        {
            return _context
                .AgentInvitations
                .NotResent()
                .Where(filterExpression)
                .ProjectToAgentInvitationResponse()
                .ToListAsync();
        }


        private readonly IUserInvitationService _invitationService;
        private readonly ICounterpartyService _counterpartyService;
        private readonly AgentInvitationOptions _options;
        private readonly EdoContext _context;
    }
}