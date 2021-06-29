using System.Security.Cryptography;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Invitations;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Invitations;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.NotificationCenter.Services;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Infrastructure;
using HappyTravel.Edo.Notifications.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Invitations
{
    public class AgentInvitationCreateService : IAgentInvitationCreateService
    {
        public AgentInvitationCreateService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ILogger<AgentInvitationCreateService> logger,
            INotificationService notificationService,
            IOptions<AgentInvitationMailOptions> options,
            IInvitationRecordService invitationRecordService,
            IAdminAgencyManagementService agencyManagementService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _notificationService = notificationService;
            _options = options.Value;
            _invitationRecordService = invitationRecordService;
            _agencyManagementService = agencyManagementService;
        }


        public Task<Result<string>> Create(UserInvitationData prefilledData, UserInvitationTypes invitationType,
            int inviterUserId, int? inviterAgencyId = null)
        {
            var invitationCode = GenerateRandomCode();
            var now = _dateTimeProvider.UtcNow();

            return SaveInvitation()
                .Tap(LogInvitationCreated)
                .Map(_ => invitationCode);


            string GenerateRandomCode()
            {
                using var provider = new RNGCryptoServiceProvider();

                var byteArray = new byte[64];
                provider.GetBytes(byteArray);

                return Base64UrlEncoder.Encode(byteArray);
            }


            async Task<Result<UserInvitation>> SaveInvitation()
            {
                var newInvitation = new UserInvitation
                {
                    CodeHash = HashGenerator.ComputeSha256(invitationCode),
                    Email = prefilledData.UserRegistrationInfo.Email,
                    Created = now,
                    InviterUserId = inviterUserId,
                    InviterAgencyId = inviterAgencyId,
                    InvitationType = invitationType,
                    InvitationStatus = UserInvitationStatuses.Active,
                    Data = JsonConvert.SerializeObject(prefilledData)
                };

                _context.UserInvitations.Add(newInvitation);

                await _context.SaveChangesAsync();

                return newInvitation;
            }


            void LogInvitationCreated()
                => _logger.LogInvitationCreated(invitationType, prefilledData.UserRegistrationInfo.Email);
        }


        public Task<Result<string>> Send(UserInvitationData prefilledData, UserInvitationTypes invitationType,
            int inviterUserId, int? inviterAgencyId = null)
        {
            return Create(prefilledData, invitationType, inviterUserId, inviterAgencyId)
                .Check(SendInvitationMailPipe);


            Task<Result> SendInvitationMailPipe(string invitationCode) 
                => SendInvitationMail(invitationCode, prefilledData, invitationType, inviterAgencyId);
        }


        public Task<Result<string>> Recreate(string oldInvitationCodeHash)
        {
            return _invitationRecordService.GetActiveInvitationByHash(oldInvitationCodeHash)
                .BindWithTransaction(_context, invitation => Result.Success(invitation)
                    .Check(SetOldInvitationResent)
                    .Bind(CreateNewInvitation));


            Task<Result> SetOldInvitationResent(UserInvitation _)
                => _invitationRecordService.SetToResent(oldInvitationCodeHash);


            Task<Result<string>> CreateNewInvitation(UserInvitation oldInvitation)
                => Create(GetInvitationData(oldInvitation), oldInvitation.InvitationType, oldInvitation.InviterUserId, oldInvitation.InviterAgencyId);
        }


        public Task<Result<string>> Resend(string oldInvitationCodeHash)
        {
            return Recreate(oldInvitationCodeHash)
                .Check(SendInvitationMailPipe);


            Task<Result> SendInvitationMailPipe(string newInvitationCode)
                => _invitationRecordService.GetActiveInvitationByCode(newInvitationCode)
                    .Bind(i => SendInvitationMail(newInvitationCode, GetInvitationData(i), i.InvitationType, i.InviterAgencyId));
        }


        private UserInvitationData GetInvitationData(UserInvitation invitation)
            => _invitationRecordService.GetInvitationData(invitation);


        private async Task<Result> SendInvitationMail(string invitationCode, UserInvitationData prefilledData,
            UserInvitationTypes invitationType, int? inviterAgencyId)
        {
            string agencyName = null;
            if (inviterAgencyId.HasValue)
            {
                var getAgencyResult = await _agencyManagementService.Get(inviterAgencyId.Value);
                if (getAgencyResult.IsFailure)
                    return Result.Failure("Could not find inviter agency");

                agencyName = getAgencyResult.Value.Name;
            }

            var messagePayload = new InvitationData
            {
                AgencyName = agencyName,
                InvitationCode = invitationCode,
                UserEmailAddress = prefilledData.UserRegistrationInfo.Email,
                UserName = $"{prefilledData.UserRegistrationInfo.FirstName} {prefilledData.UserRegistrationInfo.LastName}"
            };

            var templateId = GetTemplateId();
            if (string.IsNullOrWhiteSpace(templateId))
                return Result.Failure("Could not find invitation mail template");

            var notificationType = invitationType switch
            {
                UserInvitationTypes.Agent => NotificationTypes.AgentInvitation,
                UserInvitationTypes.ChildAgency => NotificationTypes.ChildAgencyInvitation,
                _ => NotificationTypes.None
            };

            return await _notificationService.Send(messageData: messagePayload,
                notificationType: notificationType,
                email: prefilledData.UserRegistrationInfo.Email,
                templateId: templateId);


            string GetTemplateId()
                => invitationType switch
                {
                    UserInvitationTypes.Agent => _options.AgentInvitationTemplateId,
                    UserInvitationTypes.ChildAgency => _options.ChildAgencyInvitationTemplateId,
                    _ => null
                };
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AgentInvitationCreateService> _logger;
        private readonly INotificationService _notificationService;
        private readonly AgentInvitationMailOptions _options;
        private readonly IInvitationRecordService _invitationRecordService;
        private readonly IAdminAgencyManagementService _agencyManagementService;
    }
}
