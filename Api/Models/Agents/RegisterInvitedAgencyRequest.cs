using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Users;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct RegisterInvitedAgencyRequest
    {
        [JsonConstructor]
        public RegisterInvitedAgencyRequest(UserDescriptionInfo registrationInfo, RegistrationAgencyInfo childAgencyRegistrationInfo, string invitationCode)
        {
            RegistrationInfo = registrationInfo;
            ChildAgencyRegistrationInfo = childAgencyRegistrationInfo;
            InvitationCode = invitationCode;
        }


        /// <summary>
        ///     Regular agent personal information.
        /// </summary>
        public UserDescriptionInfo RegistrationInfo { get; }

        /// <summary>
        /// Prefilled child agency registration info. Used only for child agency invitations.
        /// </summary>
        public RegistrationAgencyInfo ChildAgencyRegistrationInfo { get; }

        /// <summary>
        ///     Invitation code.
        /// </summary>
        public string InvitationCode { get; }
    }
}