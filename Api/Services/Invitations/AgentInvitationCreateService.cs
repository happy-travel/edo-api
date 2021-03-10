﻿using System;
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
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Invitations
{
    public class AgentInvitationCreateService : IAgentInvitationCreateService
    {
        public AgentInvitationCreateService(
            EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ILogger<AgentInvitationCreateService> logger,
            MailSenderWithCompanyInfo mailSender,
            IOptions<AgentInvitationMailOptions> options,
            IInvitationRecordService invitationRecordService,
            IAgencyManagementService agencyManagementService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
            _mailSender = mailSender;
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
                => _logger.LogInvitationCreated(
                    $"The invitation with type {invitationType} created for the user '{prefilledData.UserRegistrationInfo.Email}'");
        }


        public Task<Result<string>> Send(UserInvitationData prefilledData, UserInvitationTypes invitationType,
            int inviterUserId, int? inviterAgencyId = null)
        {
            return Create(prefilledData, invitationType, inviterUserId, inviterAgencyId)
                .Check(SendInvitationMail);


            async Task<Result> SendInvitationMail(string invitationCode)
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

                return await _mailSender.Send(templateId,
                    prefilledData.UserRegistrationInfo.Email,
                    messagePayload);
            }


            string GetTemplateId()
                => invitationType switch
                {
                    UserInvitationTypes.Agent => _options.AgentInvitationTemplateId,
                    UserInvitationTypes.ChildAgency => _options.ChildAgencyInvitationTemplateId,
                    _ => null
                };
        }


        public Task<Result<string>> Resend(string oldInvitationCode)
        {
            return _invitationRecordService.GetActiveInvitation(oldInvitationCode)
                .BindWithTransaction(_context, invitation => Result.Success(invitation)
                    .Check(SetOldInvitationResent)
                    .Bind(SendNewInvitation));


            Task<Result<string>> SendNewInvitation(UserInvitation oldInvitation)
                => Send(GetInvitationData(oldInvitation), oldInvitation.InvitationType, oldInvitation.InviterUserId, oldInvitation.InviterAgencyId);


            Task<Result> SetOldInvitationResent(UserInvitation _)
                => _invitationRecordService.SetToResent(oldInvitationCode);


            UserInvitationData GetInvitationData(UserInvitation invitation)
                => _invitationRecordService.GetInvitationData(invitation);
        }

        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<AgentInvitationCreateService> _logger;
        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly AgentInvitationMailOptions _options;
        private readonly IInvitationRecordService _invitationRecordService;
        private readonly IAgencyManagementService _agencyManagementService;
    }
}