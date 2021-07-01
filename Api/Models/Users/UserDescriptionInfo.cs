using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Users
{
    public readonly struct UserDescriptionInfo
    {
        [JsonConstructor]
        public UserDescriptionInfo(string title, string firstName, string lastName,
            string position, string email, int[] roleIds = null)
        {
            Title = title;
            FirstName = firstName;
            LastName = lastName;
            Position = position;
            Email = email;
            RoleIds = roleIds ?? System.Array.Empty<int>();
        }


        /// <summary>
        ///     Title, Mr, Mrs etc.
        /// </summary>
        [Required]
        public string Title { get; }

        /// <summary>
        ///     First name.
        /// </summary>
        [Required]
        public string FirstName { get; }

        /// <summary>
        ///     Last name.
        /// </summary>
        [Required]
        public string LastName { get; }

        /// <summary>
        ///     Position or designation.
        /// </summary>
        public string Position { get; }

        /// <summary>
        ///     Email
        /// </summary>
        public string Email { get; }
        
        /// <summary>
        ///     Role ids assigned to user
        /// </summary>
        public int[] RoleIds { get; }


        public override int GetHashCode()
            => (Title, FirstName, LastName, Position, Email).GetHashCode();


        public bool Equals(UserDescriptionInfo other)
            => (Title, FirstName, LastName, Position, Email) == (other.Title, other.FirstName, other.LastName, other.Position, other.Email);


        public override bool Equals(object obj)
            => obj is UserDescriptionInfo other && Equals(other);
    }
}