﻿using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Invitations;
using HappyTravel.Edo.Api.Models.Invitations;
using HappyTravel.Edo.Api.Models.Management.AuditEvents;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Infrastructure;
using HappyTravel.Edo.Data.Management;

namespace HappyTravel.Edo.Api.AdministratorServices.Invitations
{
    public class AdminInvitationAcceptService : IAdminInvitationAcceptService
    {
        public AdminInvitationAcceptService(
            IInvitationRecordService invitationRecordService,
            EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IManagementAuditService managementAuditService)
        {
            _invitationRecordService = invitationRecordService;
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _managementAuditService = managementAuditService;
        }


        public async Task<Result> Accept(string invitationCode, UserInvitationData filledData, string identity)
        {
            return await GetActiveInvitation()
                .Ensure(IsIdentityPresent, "User should have identity")
                .Ensure(IsInvitationCorrectType, "Incorrect invitation type")
                .BindWithTransaction(_context, invitation => Result.Success(invitation)
                    .Tap(SaveAccepted)
                    .Bind(CreateAdmin)
                    .Tap(WriteAuditLog));


            Task<Result<UserInvitation>> GetActiveInvitation() => _invitationRecordService.GetActiveInvitation(invitationCode);


            bool IsIdentityPresent(UserInvitation _) => !string.IsNullOrWhiteSpace(identity);


            bool IsInvitationCorrectType(UserInvitation invitation) => invitation.InvitationType == UserInvitationTypes.Administrator;


            Task SaveAccepted(UserInvitation invitation) => _invitationRecordService.SetAccepted(invitationCode);


            async Task<Result<Administrator>> CreateAdmin(UserInvitation invitation)
            {
                var now = _dateTimeProvider.UtcNow();
                var invitationData = GetData(invitation);

                var administrator = new Administrator
                {
                    Email = invitationData.UserRegistrationInfo.Email,
                    FirstName = invitationData.UserRegistrationInfo.FirstName,
                    LastName = invitationData.UserRegistrationInfo.LastName,
                    IdentityHash = HashGenerator.ComputeSha256(identity),
                    Position = invitationData.UserRegistrationInfo.Position,
                    Created = now,
                    Updated = now
                };

                _context.Administrators.Add(administrator);
                await _context.SaveChangesAsync();

                return administrator;
            }


            Task WriteAuditLog(Administrator administrator)
                => _managementAuditService.Write(ManagementEventType.AdministratorRegistration,
                    new AdministrationRegistrationEvent(administrator.Email, administrator.Id, invitationCode));


            UserInvitationData GetData(UserInvitation invitation)
                => filledData.Equals(default) ? _invitationRecordService.GetInvitationData(invitation) : filledData;
        }


        private readonly IInvitationRecordService _invitationRecordService;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IManagementAuditService _managementAuditService;
    }
}
