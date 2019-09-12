using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Emails;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Customers
{
    public class CustomerInvitationService : ICustomerInvitationService
    {
        public CustomerInvitationService(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IMailSender mailSender,
            ICustomerContext customerContext,
            IOptions<InvitationOptions> options,
            ILogger<CustomerInvitationService> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _mailSender = mailSender;
            _customerContext = customerContext;
            _logger = logger;
            _options = options.Value;
        }
        
        public async Task<Result> SendInvitation(CustomerInvitationInfo invitationInfo)
        {
            // TODO: move to authorization policies.
            if(!await _customerContext.IsMasterCustomer())
                return Result.Fail("Only master customers can send invitations");
            
            var invitationCode = GenerateRandomCode();
            var addresseeEmail = invitationInfo.Email;
            
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
                return _mailSender.Send(templateId: _options.MailTemplateId,
                    recipientAddress: addresseeEmail, 
                    messageData: new InvitationData { InvitationCode = invitationCode });
            }

            Task SaveInvitationData()
            {
                _context.UserInvitations.Add(new UserInvitation
                {
                    CodeHash = HashGenerator.ComputeHash(invitationCode),
                    Created = _dateTimeProvider.UtcNow(),
                    Data = JsonConvert.SerializeObject(invitationInfo),
                    Email = addresseeEmail
                });

                return _context.SaveChangesAsync();
            }
            
            void LogInvitationCreated() => _logger
                    .LogInvitationCreatedInformation(
                        message: $"Invitation for user {invitationInfo.Email} created");
        }

        public async Task AcceptInvitation(string invitationCode)
        {
            var invitationMaybe = await GetCustomerInvitation(invitationCode);
            if (invitationMaybe.HasValue)
            {
                var invitation = invitationMaybe.Value;
                invitation.IsAccepted = true;
                _context.Update(invitation);
                await _context.SaveChangesAsync();
            }
        }

        public Task<Result<CustomerInvitationInfo>> GetPendingInvitation(string invitationCode)
        {
            return GetCustomerInvitation(invitationCode).ToResult("Could not find")
                .Ensure(IsNotAccepted, "Already accepted")
                .Ensure(InvitationIsActual, "Invitation expired")
                .OnSuccess(GetInvitationData);
            
              bool InvitationIsActual(UserInvitation invitation)
              {
                  return invitation.Created + _options.InvitationExpirationPeriod > _dateTimeProvider.UtcNow();
              }

              bool IsNotAccepted(UserInvitation invitation)
              {
                  return !invitation.IsAccepted;
              }
        }
        
        private async Task<Maybe<UserInvitation>> GetCustomerInvitation(string code)
        {
            var invitation = await _context.UserInvitations
                .SingleOrDefaultAsync(c => c.CodeHash == HashGenerator.ComputeHash(code));

            return invitation ?? Maybe<UserInvitation>.None;
        }

        private static CustomerInvitationInfo GetInvitationData(UserInvitation customerInvitation)
        {
            return JsonConvert.DeserializeObject<CustomerInvitationInfo>(customerInvitation.Data);
        }
        
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMailSender _mailSender;
        private readonly ICustomerContext _customerContext;
        private readonly ILogger<CustomerInvitationService> _logger;
        private readonly InvitationOptions _options;
    }
}