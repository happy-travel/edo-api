using System;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Invitations;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Extensions
{
    public static class InvitationInfoExtensions
    {
        public static UserInvitationData ToUserInvitationData(this UserDescriptionInfo info, string email = null)
        {
            var newInfo = new UserDescriptionInfo(info.Title, info.FirstName, info.LastName, info.Position, email ?? info.Email);
            return new UserInvitationData(newInfo, default);
        }


        public static UserInvitationData ToUserInvitationData(this RegisterInvitedAgencyRequest request)
            => new UserInvitationData(request.RegistrationInfo, request.ChildAgencyRegistrationInfo);


        public static UserInvitationData ToUserInvitationData(this CreateChildAgencyInvitationRequest request)
            => new UserInvitationData(request.UserRegistrationInfo,
                new RegistrationAgencyInfo(
                    request.ChildAgencyRegistrationInfo.Name,
                    request.ChildAgencyRegistrationInfo.Address,
                    request.ChildAgencyRegistrationInfo.BillingEmail,
                    request.ChildAgencyRegistrationInfo.City,
                    request.ChildAgencyRegistrationInfo.CountryCode,
                    request.ChildAgencyRegistrationInfo.Fax,
                    request.ChildAgencyRegistrationInfo.Phone,
                    request.ChildAgencyRegistrationInfo.PostalCode,
                    request.ChildAgencyRegistrationInfo.Website,
                    request.ChildAgencyRegistrationInfo.VatNumber));
    }
}
