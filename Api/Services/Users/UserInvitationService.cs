using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HappyTravel.Edo.Api.Services.Users
{
    public class UserInvitationService : IUserInvitationService
    {
        public UserInvitationService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            MailSenderWithCompanyInfo mailSender,
            ILogger<UserInvitationService> logger,
            IOptions<UserInvitationOptions> options)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _mailSender = mailSender;
            _logger = logger;
            _options = options.Value;
        }


        public async Task<Result> Send<TInvitationData>(string email,
            TInvitationData invitationInfo,
            Func<TInvitationData, string, DataWithCompanyInfo> messagePayloadGenerator, 
            string mailTemplateId,
            UserInvitationTypes invitationType)
        {
            var invitationCode = GenerateRandomCode();

            return await SendInvitationMail()
                .Tap(SaveInvitation)
                .Tap(LogInvitationCreated);

            Task<Result> SendInvitationMail()
            {
                var messagePayload = messagePayloadGenerator(invitationInfo, invitationCode);
                
                return _mailSender.Send(mailTemplateId,
                    email,
                    messagePayload);
            }

            Task SaveInvitation() => SaveInvitationData(email, invitationInfo, invitationType, invitationCode);
            
            void LogInvitationCreated() => this.LogInvitationCreated(email);
        }


        public Task<Result<string>> Create<TInvitationData>(string email, TInvitationData invitationInfo, UserInvitationTypes invitationType)
        {
            var invitationCode = GenerateRandomCode();

            return SaveInvitation()
                .Tap(LogInvitationCreated)
                .Map(ReturnCode);
            
            async Task<Result> SaveInvitation()
            {
                await SaveInvitationData(email, invitationInfo, invitationType, invitationCode);
                return Result.Success();
            }

            void LogInvitationCreated() => this.LogInvitationCreated(email);
            
            string ReturnCode() => invitationCode;
        }


        public async Task Accept(string invitationCode)
        {
            var invitationMaybe = await GetInvitation(invitationCode);
            if (invitationMaybe.HasValue)
            {
                var invitation = invitationMaybe.Value;
                invitation.IsAccepted = true;
                _context.Update(invitation);
                await _context.SaveChangesAsync();
            }
        }


        public Task<Result<TInvitationData>> GetPendingInvitation<TInvitationData>(string invitationCode, UserInvitationTypes invitationType)
        {
            return GetInvitation(invitationCode).ToResult("Could not find invitation")
                .Ensure(IsNotAccepted, "Already accepted")
                .Ensure(IsNotResent, "Already resent")
                .Ensure(HasCorrectType, "Invitation type mismatch")
                .Ensure(InvitationIsActual, "Invitation expired")
                .Map(GetInvitationData<TInvitationData>);

            static bool IsNotAccepted(InvitationBase invitation) => !invitation.IsAccepted;

            static bool IsNotResent(InvitationBase invitation) => invitation.InvitationType switch
            {
                UserInvitationTypes.Agent => !((AgentInvitation) invitation).IsResent,
                UserInvitationTypes.Administrator => true,
                _ => throw new NotImplementedException($"{Formatters.EnumFormatters.FromDescription(invitation.InvitationType)} not supported")
            };

            bool HasCorrectType(InvitationBase invitation) => invitation.InvitationType == invitationType;

            bool InvitationIsActual(InvitationBase invitation) => invitation.Created + _options.InvitationExpirationPeriod > _dateTimeProvider.UtcNow();
        }


        private static TInvitationData GetInvitationData<TInvitationData>(InvitationBase invitation)
        {
            return invitation.InvitationType switch
            {
                UserInvitationTypes.Agent => MapInvitationData<TInvitationData>((AgentInvitation) invitation),
                UserInvitationTypes.Administrator =>MapInvitationData<TInvitationData>((AdminInvitation) invitation),
                _ => throw new NotImplementedException($"{Formatters.EnumFormatters.FromDescription(invitation.InvitationType)} not supported")
            };
        }


        private static TInvitationData MapInvitationData<TInvitationData>(AdminInvitation invitation)
        {
            return (TInvitationData) Convert.ChangeType(new AdministratorInvitationInfo(
                invitation.Email,
                invitation.Data.LastName,
                invitation.Data.FirstName,
                invitation.Data.Position,
                invitation.Data.Title),
                typeof(TInvitationData));
        }


        private static TInvitationData MapInvitationData<TInvitationData>(AgentInvitation invitation)
        {
            var data = invitation.Data.RegistrationInfo;
            var agentEditableInfo = new AgentEditableInfo(data.Title, data.FirstName, data.LastName, data.Position, invitation.Email);
            return (TInvitationData) Convert.ChangeType(new AgentInvitationInfo(
                agentEditableInfo,
                invitation.Data.AgencyId,
                invitation.Data.AgentId,
                invitation.Email), typeof(TInvitationData));
        }


        private async Task<Maybe<InvitationBase>> GetInvitation(string code)
        {
            var invitation = await _context.UserInvitations
                .SingleOrDefaultAsync(c => c.CodeHash == HashGenerator.ComputeSha256(code));

            return invitation ?? Maybe<InvitationBase>.None;
        }
        
        
        private string GenerateRandomCode()
        {
            using var provider = new RNGCryptoServiceProvider();
                
            var byteArray = new byte[64];
            provider.GetBytes(byteArray);

            return Base64UrlEncoder.Encode(byteArray);
        }
        
        
        private Task SaveInvitationData<TInvitationData>(string addresseeEmail, TInvitationData invitationInfo, 
            UserInvitationTypes invitationType, string invitationCode)
        {
            switch (invitationInfo)
            {
                case AgentInvitationInfo info when invitationType == UserInvitationTypes.Agent:
                {
                    _context.AgentInvitations.Add(new AgentInvitation
                    {
                        CodeHash = HashGenerator.ComputeSha256(invitationCode),
                        Created = _dateTimeProvider.UtcNow(),
                        Data = new AgentInvitation.AgentInvitationData
                        {
                            AgencyId = info.AgencyId,
                            AgentId = info.AgentId,
                            Email = addresseeEmail,
                            RegistrationInfo = new AgentInvitation.AgentRegistrationInfo
                            {
                                FirstName = info.RegistrationInfo.FirstName,
                                LastName = info.RegistrationInfo.LastName,
                                Title = info.RegistrationInfo.Title,
                                Position = info.RegistrationInfo.Position
                            }
                        },
                        Email = addresseeEmail,
                        InvitationType = invitationType
                    });
                }
                    break;

                case AdministratorInvitationInfo info when invitationType == UserInvitationTypes.Administrator:
                {
                    _context.AdminInvitations.Add(new AdminInvitation
                    {
                        CodeHash = HashGenerator.ComputeSha256(invitationCode),
                        Created = _dateTimeProvider.UtcNow(),
                        Data = new AdminInvitation.AdminInvitationData
                        {
                            Title = info.Title,
                            Email = addresseeEmail,
                            FirstName = info.FirstName,
                            LastName = info.LastName
                        },
                        Email = addresseeEmail,
                        InvitationType = invitationType
                    });
                }
                    break;
            }

            return _context.SaveChangesAsync();
        }
        
        
        private void LogInvitationCreated(string email)
            => _logger.LogInvitationCreated(
                $"The invitation created for the user '{email}'");


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger<UserInvitationService> _logger;
        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly UserInvitationOptions _options;
    }
}