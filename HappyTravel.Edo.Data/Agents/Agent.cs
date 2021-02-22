using System;

namespace HappyTravel.Edo.Data.Agents
{
    public class Agent
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public string Position { get; set; }
        public string Email { get; set; }
        public string IdentityHash { get; set; }
        public string AppSettings { get; set; }
        public string UserSettings { get; set; }
        public DateTime Created { get; set; }
    }
}