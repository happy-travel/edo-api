using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct AgentInvitationInfo
    {
        [JsonConstructor]
        public AgentInvitationInfo(UserDescriptionInfo userRegistrationInfo, RegistrationAgencyInfo? childAgencyRegistrationInfo,
            UserInvitationTypes userInvitationType, int agencyId, int agentId, string email)
        {
            UserRegistrationInfo = userRegistrationInfo;
            ChildAgencyRegistrationInfo = childAgencyRegistrationInfo;
            UserInvitationType = userInvitationType;
            AgencyId = agencyId;
            Email = email;
            AgentId = agentId;
        }

        /// <summary>
        /// Prefilled user registration info.
        /// </summary>
        [Required]
        public UserDescriptionInfo UserRegistrationInfo { get; }

        /// <summary>
        /// Prefilled child agency registration info. Used only for child agency invitations.
        /// </summary>
        public RegistrationAgencyInfo? ChildAgencyRegistrationInfo { get; }

        /// <summary>
        /// Type of the invitation
        /// </summary>
        public UserInvitationTypes UserInvitationType { get; }

        /// <summary>
        ///     Inviter agency id.
        /// </summary>
        [Required]
        public int AgencyId { get; }

        /// <summary>
        ///    Inviter agent id
        /// </summary>
        [Required]
        public int AgentId { get; }

        /// <summary>
        ///     E-mail for invitation.
        /// </summary>
        [Required]
        public string Email { get; }
    }
}