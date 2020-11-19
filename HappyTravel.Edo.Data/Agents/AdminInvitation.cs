namespace HappyTravel.Edo.Data.Agents
{
    public class AdminInvitation : InvitationBase
    {
        public AdminInvitationData Data { get; set; }


        public class AdminInvitationData
        {
            public string Title { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Position { get; set; }
            public string Email { get; set; }
        }
    }
}