namespace HappyTravel.Edo.Api.Models.Management.Administrators
{
    public readonly struct AdministratorInfo
    {
        public AdministratorInfo(int id, string firstName, 
            string lastName, string position)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Position = position;
        }
        
        /// <summary>
        /// Id of administrator
        /// </summary>
        public int Id { get; }
        
        /// <summary>
        /// First name
        /// </summary>
        public string FirstName { get; }
        
        /// <summary>
        /// Last name
        /// </summary>
        public string LastName { get; }
        
        /// <summary>
        /// Position
        /// </summary>
        public string Position { get; }
    }
}