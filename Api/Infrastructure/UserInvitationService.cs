using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Emails;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public class UserInvitationService : IUserInvitationService
    {
        public UserInvitationService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IMailSender mailSender,
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
            string mailTemplateId,
            UserInvitationTypes invitationType)
        {
            var invitationCode = GenerateRandomCode();
            var addresseeEmail = email;
            
            return await SendInvitationMail()
                .OnSuccess(SaveInvitationData)
                .OnSuccess(LogInvitationCreated);
            
            string GenerateRandomCode()
            {
                using (RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider())
                {
                    var byteArray = new byte[64];
                    provider.GetBytes(byteArray);
                    return Convert.ToBase64String(byteArray)
                        .Replace("/", string.Empty);
                }
            }

            Task<Result> SendInvitationMail()
            {
                return _mailSender.Send(templateId: mailTemplateId,
                    recipientAddress: addresseeEmail, 
                    messageData: new InvitationData { InvitationCode = invitationCode, UserEmailAddress = addresseeEmail });
            }

            Task SaveInvitationData()
            {
                _context.UserInvitations.Add(new UserInvitation
                {
                    CodeHash = HashGenerator.ComputeHash(invitationCode),
                    Created = _dateTimeProvider.UtcNow(),
                    Data = JsonConvert.SerializeObject(invitationInfo),
                    Email = addresseeEmail,
                    InvitationType = invitationType
                });

                return _context.SaveChangesAsync();
            }
            
            void LogInvitationCreated() => _logger.LogInvitationCreatedInformation(
                message: $"Invitation for user {email} created");
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
                .Ensure(HasCorrectType, "Invitation type missmatch")
                .Ensure(InvitationIsActual, "Invitation expired")
                .OnSuccess(GetInvitationData<TInvitationData>);
            
            bool IsNotAccepted(UserInvitation invitation)
            {
                return !invitation.IsAccepted;
            }
            
            bool HasCorrectType(UserInvitation invitation)
            {
                return invitation.InvitationType == invitationType;
            }
            
            bool InvitationIsActual(UserInvitation invitation)
            {
                return invitation.Created + _options.InvitationExpirationPeriod > _dateTimeProvider.UtcNow();
            }
        }

        private static TInvitationData GetInvitationData<TInvitationData>(UserInvitation invitation)
        {
            return JsonConvert.DeserializeObject<TInvitationData>(invitation.Data);
        }
        
        private async Task<Maybe<UserInvitation>> GetInvitation(string code)
        {
            var invitation = await _context.UserInvitations
                .SingleOrDefaultAsync(c => c.CodeHash == HashGenerator.ComputeHash(code));

            return invitation ?? Maybe<UserInvitation>.None;
        }
        
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMailSender _mailSender;
        private readonly ILogger<UserInvitationService> _logger;
        private readonly UserInvitationOptions _options;
    }
}