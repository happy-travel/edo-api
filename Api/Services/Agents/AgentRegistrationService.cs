using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgentRegistrationService : IAgentRegistrationService
    {
        public AgentRegistrationService(EdoContext context,
            ICounterpartyService counterpartyService,
            IAgentService agentService,
            IAgentInvitationService agentInvitationService,
            IOptions<AgentRegistrationNotificationOptions> notificationOptions,
            MailSenderWithCompanyInfo mailSender,
            ILogger<AgentRegistrationService> logger)
        {
            _context = context;
            _counterpartyService = counterpartyService;
            _agentService = agentService;
            _agentInvitationService = agentInvitationService;
            _notificationOptions = notificationOptions.Value;
            _mailSender = mailSender;
            _logger = logger;
        }


        public Task<Result> RegisterWithCounterparty(AgentEditableInfo agentData, CounterpartyEditRequest counterpartyData, string externalIdentity,
            string email)
        {
            return Result.Success()
                .Ensure(IsIdentityPresent, "User should have identity")
                .BindWithTransaction(_context, () => Result.Success()
                    .Bind(CreateCounterparty)
                    .Bind(CreateAgent)
                    .Tap(AddMasterCounterpartyRelation))
                .Bind(LogSuccess)
                .Bind(SendRegistrationMailToAdmins)
                .OnFailure(LogFailure);

            bool IsIdentityPresent() => !string.IsNullOrWhiteSpace(externalIdentity);
            

            Task<Result<CounterpartyInfo>> CreateCounterparty() => _counterpartyService.Add(counterpartyData);


            async Task<Result<(CounterpartyInfo, Agent)>> CreateAgent(CounterpartyInfo counterparty)
            {
                var (_, isFailure, agent, error) = await _agentService.Add(agentData, externalIdentity, email);
                return isFailure
                    ? Result.Failure<(CounterpartyInfo, Agent)>(error)
                    : Result.Success((counterparty1: counterparty, agent));
            }


            async Task AddMasterCounterpartyRelation((CounterpartyInfo counterparty, Agent agent) counterpartyUserInfo)
            {
                var (counterparty, agent) = counterpartyUserInfo;
                var defaultAgency = await _counterpartyService.GetDefaultAgency(counterparty.Id);
                await AddCounterpartyRelation(agent,
                    AgentAgencyRelationTypes.Master,
                    PermissionSets.Master,
                    defaultAgency.Id);
            }


            async Task<Result> SendRegistrationMailToAdmins(CounterpartyInfo counterpartyInfo)
            {
                var agent = $"{agentData.Title} {agentData.FirstName} {agentData.LastName}";
                if (!string.IsNullOrWhiteSpace(agentData.Position))
                    agent += $" ({agentData.Position})";

                var messageData = new RegistrationDataForAdmin
                {
                    Counterparty =counterpartyInfo,
                    AgentEmail = email,
                    AgentName = agent
                };

                return await _mailSender.Send(_notificationOptions.MasterAgentMailTemplateId, _notificationOptions.AdministratorsEmails, messageData);
            }


            Result<CounterpartyInfo> LogSuccess((CounterpartyInfo, Agent) registrationData)
            {
                var (counterparty, agent) = registrationData;
                _logger.LogAgentRegistrationSuccess($"Agent {agent.Email} with counterparty '{counterparty.Name}' successfully registered");
                return Result.Success(counterparty);
            }


            void LogFailure(string error)
            {
                _logger.LogAgentRegistrationFailed(error);
            }
        }


        public Task<Result> RegisterInvited(AgentEditableInfo registrationInfo, string invitationCode, string externalIdentity, string email)
        {
            return Result.Success()
                .Ensure(IsIdentityPresent, "User should have identity")
                .Bind(GetPendingInvitation)
                .Ensure(IsEmailUnique, "Agent with this email already exists")
                .BindWithTransaction(_context, invitation => Result.Success(invitation)
                    .Bind(CreateAgent)
                    .Tap(AddRegularCounterpartyRelation)
                    .Map(AcceptInvitation))
                .Bind(LogSuccess)
                .Bind(GetMasterAgent)
                .Bind(SendRegistrationMailToMaster)
                .OnFailure(LogFailed);


            async Task<bool> IsEmailUnique(AgentInvitationInfo info) => !await _context.Agents.AnyAsync(a => a.Email == info.Email);


            async Task<AgentInvitationInfo> AcceptInvitation((AgentInvitationInfo invitationInfo, Agent agent) invitationData)
            {
                await _agentInvitationService.Accept(invitationCode);
                return invitationData.invitationInfo;
            }


            async Task AddRegularCounterpartyRelation((AgentInvitationInfo, Agent) invitationData)
            {
                var (invitation, agent) = invitationData;

                await AddCounterpartyRelation(agent, AgentAgencyRelationTypes.Regular, PermissionSets.Default, invitation.AgencyId);
            }


            async Task<Result<(AgentInvitationInfo, Agent)>> CreateAgent(AgentInvitationInfo invitation)
            {
                var (_, isFailure, agent, error) = await _agentService.Add(registrationInfo, externalIdentity, email);
                return isFailure
                    ? Result.Failure<(AgentInvitationInfo, Agent)>(error)
                    : Result.Success((invitation, agent));
            }

            Task<Result<Agent>> GetMasterAgent(AgentInvitationInfo invitationInfo) => _agentService.GetMasterAgent(invitationInfo.AgencyId);

            Task<Result<AgentInvitationInfo>> GetPendingInvitation() => _agentInvitationService.GetPendingInvitation(invitationCode);

            bool IsIdentityPresent() => !string.IsNullOrWhiteSpace(externalIdentity);


            void LogFailed(string error)
            {
                _logger.LogAgentRegistrationFailed(error);
            }


            Result<AgentInvitationInfo> LogSuccess(AgentInvitationInfo invitationInfo)
            {
                _logger.LogAgentRegistrationSuccess($"Agent {email} successfully registered and bound to agency ID:'{invitationInfo.AgencyId}'");
                return Result.Success(invitationInfo);
            }


            async Task<Result> SendRegistrationMailToMaster(Agent master)
            {
                var position = registrationInfo.Position;
                if (string.IsNullOrWhiteSpace(position))
                    position = "a new employee";

                var (_, isFailure, error) = await _mailSender.Send(_notificationOptions.RegularAgentMailTemplateId, master.Email, new RegistrationDataForMaster
                {
                    AgentName = $"{registrationInfo.FirstName} {registrationInfo.LastName}",
                    Position = position,
                    Title = registrationInfo.Title
                });
                if (isFailure)
                    return Result.Failure(error);

                return Result.Success();
            }
        }


        private Task AddCounterpartyRelation(Agent agent, AgentAgencyRelationTypes relationType, InAgencyPermissions permissions, int agencyId)
        {
            _context.AgentAgencyRelations.Add(new AgentAgencyRelation
            {
                AgentId = agent.Id,
                Type = relationType,
                InAgencyPermissions = permissions,
                AgencyId = agencyId,
                IsActive = true
            });

            return _context.SaveChangesAsync();
        }


        private readonly ICounterpartyService _counterpartyService;
        private readonly EdoContext _context;
        private readonly IAgentInvitationService _agentInvitationService;
        private readonly IAgentService _agentService;
        private readonly ILogger<AgentRegistrationService> _logger;
        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly AgentRegistrationNotificationOptions _notificationOptions;
    }
}