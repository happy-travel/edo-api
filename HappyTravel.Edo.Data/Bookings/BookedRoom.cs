using System;
using System.Collections.Generic;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Data.Bookings
{
    public class BookedRoom
    {
        // EF constructor
        private BookedRoom()
        {
        }
        
        public BookedRoom(RoomTypes type, bool isExtraBedNeeded, MoneyAmount price, BoardBasisTypes boardBasis, string mealPlan, 
            DateTime? deadlineDate, string contractDescription, List<KeyValuePair<string, string>> remarks, Deadline deadlineDetails, List<Passenger> passengers,
            string supplierRoomReferenceCode)
        {
            Type = type;
            Passengers = passengers;
            SupplierRoomReferenceCode = supplierRoomReferenceCode;
            IsExtraBedNeeded = isExtraBedNeeded;
            Price = price;
            BoardBasis = boardBasis;
            MealPlan = mealPlan;
            DeadlineDate = deadlineDate;
            ContractDescription = contractDescription;
            Remarks = remarks ?? new List<KeyValuePair<string, string>>(0);
            DeadlineDetails = deadlineDetails;
            Passengers = passengers ?? new List<Passenger>(0);
        }


        public BookedRoom(BookedRoom room, string supplierRoomReferenceCode) : this(room.Type, room.IsExtraBedNeeded, room.Price,
            room.BoardBasis, room.MealPlan, room.DeadlineDate, room.ContractDescription, room.Remarks, room.DeadlineDetails, room.Passengers,
            supplierRoomReferenceCode)
        { }
        

        public BoardBasisTypes BoardBasis { get; set; }
        public string MealPlan { get; set;}
        public DateTime? DeadlineDate { get; set;}
        public string ContractDescription { get; set;}
        public List<KeyValuePair<string, string>> Remarks { get; set;}
        public Deadline DeadlineDetails { get; set;}
        public RoomTypes Type { get; set;}
        public bool IsExtraBedNeeded { get; set;}
        public MoneyAmount Price { get; set;}
        public List<Passenger> Passengers { get; set;}
        public string SupplierRoomReferenceCode { get; set;}
    }
}