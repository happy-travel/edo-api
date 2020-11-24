namespace HappyTravel.Edo.Data.Agents
{
    public class AgentInvitation : InvitationBase
    {
        public AgentInvitationData Data { get; set; }
        public bool IsResent { get; set; }


        public class AgentInvitationData
        {
            public AgentRegistrationInfo RegistrationInfo { get; set; }
            public int AgentId { get; set; }
            public int AgencyId { get; set; }
            public string Email { get; set; }
        }


        public class AgentRegistrationInfo
        {
            public string Title { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Position { get; set; }
        }
    }
}