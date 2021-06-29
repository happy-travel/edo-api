using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Invitations;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Invitations;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.NotificationCenter.Services;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Infrastructure;
using HappyTravel.Edo.Notifications.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Invitations
{
    public class AgentInvitationAcceptService : IAgentInvitationAcceptService
    {
        public AgentInvitationAcceptService(
            EdoContext context,
            INotificationService notificationService,
            ILogger<AgentInvitationAcceptService> logger,
            IAdminAgencyManagementService agencyManagementService,
            Agents.IAgentService agentService,
            IOptions<AgentRegistrationNotificationOptions> notificationOptions,
            IInvitationRecordService invitationRecordService,
            IAccountManagementService accountManagementService)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
            _agencyManagementService = agencyManagementService;
            _agentService = agentService;
            _notificationOptions = notificationOptions.Value;
            _invitationRecordService = invitationRecordService;
            _accountManagementService = accountManagementService;
        }


        public async Task<Result> Accept(string invitationCode, UserInvitationData filledData, string identity, string email)
        {
            return await GetActiveInvitation()
                .Bind(Validate)
                .BindWithTransaction(_context, values => Result.Success(values)
                    .Tap(SaveAccepted)
                    .Bind(CreateAgent)
                    .Bind(GetOrCreateAgency)
                    .Tap(AddAgentAgencyRelation))
                .Tap(LogSuccess)
                .Tap(SendRegistrationMailToMaster)
                .OnFailure(LogFailed);


            async Task<Result<AcceptPipeValues>> GetActiveInvitation()
            {
                var (_, isFailure, invitation, error) = await _invitationRecordService.GetActiveInvitationByCode(invitationCode);
                if (isFailure)
                    return Result.Failure<AcceptPipeValues>(error);

                return new AcceptPipeValues
                {
                    Invitation = invitation,
                    InvitationData = filledData.Equals(default) ? _invitationRecordService.GetInvitationData(invitation) : filledData
                };
            }


            Task<Result<AcceptPipeValues>> Validate(AcceptPipeValues values)
                => Result.Success(values)
                    .Ensure(IsIdentityPresent, "User should have identity")
                    .Ensure(IsInvitationTypeCorrect, "Incorrect invitation type")
                    .Ensure(IsAgencyIdFilled, "Could not find inviter's agency id")
                    .Ensure(IsEmailFilled, "Agent email required")
                    .Ensure(IsAgentEmailUnique, "Agent with this email already exists");


            bool IsIdentityPresent(AcceptPipeValues _)
                => !string.IsNullOrWhiteSpace(identity);


            bool IsInvitationTypeCorrect(AcceptPipeValues values) 
                => values.Invitation.InvitationType == UserInvitationTypes.Agent || values.Invitation.InvitationType == UserInvitationTypes.ChildAgency;


            bool IsAgencyIdFilled(AcceptPipeValues values)
                => values.Invitation.InviterAgencyId.HasValue;


            bool IsEmailFilled(AcceptPipeValues values)
                => !string.IsNullOrWhiteSpace(email);


            async Task<bool> IsAgentEmailUnique(AcceptPipeValues values)
                => !await _context.Agents.AnyAsync(a => a.Email == email);


            Task SaveAccepted(AcceptPipeValues _)
                => _invitationRecordService.SetAccepted(invitationCode);


            async Task<Result<AcceptPipeValues>> CreateAgent(AcceptPipeValues values)
            {
                var (_, isFailure, agent, error) = await _agentService.Add(values.InvitationData.UserRegistrationInfo, identity, email);

                if (isFailure)
                    return Result.Failure<AcceptPipeValues>(error);

                values.Agent = agent;
                return values;
            }


            async Task<Result<AcceptPipeValues>> GetOrCreateAgency(AcceptPipeValues values)
            {
                if (values.Invitation.InvitationType == UserInvitationTypes.Agent)
                {
                    values.AgencyId = values.Invitation.InviterAgencyId.Value;
                    values.Permissions = PermissionSets.Default;
                    values.RelationType = AgentAgencyRelationTypes.Regular;
                    values.NotificationTemplateId = _notificationOptions.RegularAgentMailTemplateId;
                    values.NotificationType = NotificationTypes.AgentSuccessfulRegistration;

                    return values;
                }

                var (_, isGetAgencyFailure, inviterAgency, agencyError) =
                    await _agencyManagementService.Get(values.Invitation.InviterAgencyId.Value);
                if (isGetAgencyFailure)
                    return Result.Failure<AcceptPipeValues>(agencyError);

                var (_, isValidationFailure, validationError) = AgencyValidator.Validate(values.InvitationData.ChildAgencyRegistrationInfo);
                if (isValidationFailure)
                    return Result.Failure<AcceptPipeValues>(validationError);

                var childAgency = await _agencyManagementService.Create(
                    values.InvitationData.ChildAgencyRegistrationInfo,
                    counterpartyId: inviterAgency.CounterpartyId.Value,
                    parentAgencyId: inviterAgency.Id);

                var childAgencyRecord = await _context.Agencies.SingleAsync(a => a.Id == childAgency.Id.Value);
                await _accountManagementService.CreateForAgency(childAgencyRecord, childAgencyRecord.PreferredCurrency);

                values.AgencyName = childAgency.Name;
                values.AgencyId = childAgency.Id.Value;
                values.Permissions = PermissionSets.Master;
                values.RelationType = AgentAgencyRelationTypes.Master;
                values.NotificationTemplateId = _notificationOptions.ChildAgencyMailTemplateId;
                values.NotificationType = NotificationTypes.ChildAgencySuccessfulRegistration;

                return values;
            }


            Task AddAgentAgencyRelation(AcceptPipeValues values)
            {
                _context.AgentAgencyRelations.Add(new AgentAgencyRelation
                {
                    AgentId = values.Agent.Id,
                    Type = values.RelationType,
                    InAgencyPermissions = values.Permissions,
                    AgencyId = values.AgencyId,
                    IsActive = true
                });

                return _context.SaveChangesAsync();
            }


            void LogSuccess(AcceptPipeValues values)
                => _logger.LogAgentRegistrationSuccess(email);


            async Task SendRegistrationMailToMaster(AcceptPipeValues values)
            {
                var (_, isGetMasterFailure, master, getMasterError) = await _agentService.GetMasterAgent(values.Invitation.InviterAgencyId.Value);
                if (isGetMasterFailure)
                {
                    LogNotificationFailed(getMasterError);
                    return;
                }

                var registrationInfo = values.InvitationData.UserRegistrationInfo;
                var position = string.IsNullOrWhiteSpace(registrationInfo.Position)
                    ? "a new employee"
                    : registrationInfo.Position;

                var registrationData = new RegistrationData
                {
                    AgentName = $"{registrationInfo.FirstName} {registrationInfo.LastName}",
                    Position = position,
                    Title = registrationInfo.Title,
                    AgencyName = values.AgencyName
                };

                var (_, isNotificationFailure, notificationError) 
                    = await _notificationService.Send(agent: new SlimAgentContext(master.Id, values.Invitation.InviterAgencyId.Value),
                        messageData: registrationData,
                        notificationType: values.NotificationType,
                        email: master.Email,
                        templateId: values.NotificationTemplateId);

                if (isNotificationFailure)
                    LogNotificationFailed(notificationError);
            }


            void LogFailed(string error)
                => _logger.LogAgentRegistrationFailed(error);


            void LogNotificationFailed(string error)
                => _logger.LogAgentRegistrationNotificationFailure(error);
        }


        private struct AcceptPipeValues
        {
            public UserInvitation Invitation { get; set; }
            public UserInvitationData InvitationData { get; set; }
            public Agent Agent { get; set; }
            public int AgencyId { get; set; }
            public string AgencyName { get; set; }
            public InAgencyPermissions Permissions { get; set; }
            public AgentAgencyRelationTypes RelationType { get; set; }
            public string NotificationTemplateId { get; set; }
            public NotificationTypes NotificationType { get; set; }
        }


        private readonly EdoContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AgentInvitationAcceptService> _logger;
        private readonly IAdminAgencyManagementService _agencyManagementService;
        private readonly Agents.IAgentService _agentService;
        private readonly AgentRegistrationNotificationOptions _notificationOptions;
        private readonly IInvitationRecordService _invitationRecordService;
        private readonly IAccountManagementService _accountManagementService;
    }
}
