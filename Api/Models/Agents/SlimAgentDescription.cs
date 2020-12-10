namespace HappyTravel.Edo.Api.Models.Agents
{
    public readonly struct SlimAgentDescription
    {
        public SlimAgentDescription(int id, string firstName, string lastName, string position)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Position = position;
        }

        /// <summary>
        /// Agent id
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Agent's First Name
        /// </summary>
        public string FirstName { get; init; }
        
        // Agent's Last Name
        public string LastName { get; init; }
        
        /// <summary>
        /// Agent's position
        /// </summary>
        public string Position { get; init; }
    }
}