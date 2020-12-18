using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Data.Bookings
{
    public class Passenger
    {
        // EF constructor
        private Passenger() { }


        public Passenger(PassengerTitles title, string lastName, string firstName, bool isLeader, int? age)
        {
            Title = title;
            LastName = lastName;
            FirstName = firstName;
            IsLeader = isLeader;
            Age = age;
        }

        public int? Age { get; set; }
        public string FirstName { get; set; }
        public bool IsLeader { get; set; }
        public string LastName { get; set; }
        public PassengerTitles Title { get; set; }
    }
}