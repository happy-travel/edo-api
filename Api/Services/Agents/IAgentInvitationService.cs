using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Data.Agents;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public interface IAgentInvitationService
    {
        Task<Result> Send(SendAgentInvitationRequest sendAgentInvitationRequest, AgentContext agentContext);

        Task Accept(string invitationCode);

        Task<Result<AgentInvitationInfo>> GetPendingInvitation(string invitationCode);

        Task<Result<string>> Create(SendAgentInvitationRequest sendAgentInvitationRequest, AgentContext agentContext);

        Task<List<AgentInvitationResponse>> GetAgencyInvitations(int agencyId);

        Task<List<AgentInvitationResponse>> GetAgentInvitations(int agencyId);

        Task<Result> Resend(string invitationId, AgentContext agent);
    }
}