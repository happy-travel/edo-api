using System;
using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Api.Models.Users;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Invitations
{
    public readonly struct CreateChildAgencyInvitationRequest
    {
        [JsonConstructor]
        public CreateChildAgencyInvitationRequest(UserDescriptionInfo userRegistrationInfo,
            // Think about removing such a special property from there
            ChildAgencyInvitationInfo childAgencyRegistrationInfo)
        {
            UserRegistrationInfo = userRegistrationInfo;
            ChildAgencyRegistrationInfo = childAgencyRegistrationInfo;
        }


        /// <summary>
        /// Prefilled user registration info.
        /// </summary>
        [Required]
        public UserDescriptionInfo UserRegistrationInfo { get; }

        /// <summary>
        /// Prefilled child agency registration info. Used only for child agency invitations.
        /// </summary>
        public ChildAgencyInvitationInfo ChildAgencyRegistrationInfo { get; }


        public override int GetHashCode()
            => (UserRegistrationInfo, ChildAgencyRegistrationInfo).GetHashCode();


        public bool Equals(CreateChildAgencyInvitationRequest other)
            => UserRegistrationInfo.Equals(other.UserRegistrationInfo) && ChildAgencyRegistrationInfo.Equals(other.ChildAgencyRegistrationInfo);


        public override bool Equals(object obj)
            => obj is CreateChildAgencyInvitationRequest other && Equals(other);
    }
}
