using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Agents
{
    public abstract class InvitationBase
    {
        public string CodeHash { get; set; }
        public string Email { get; set; }
        public DateTime Created { get; set; }
        public bool IsAccepted { get; set; }
        public UserInvitationTypes InvitationType { get; set; }
    }
}