using System;
using System.Collections.Generic;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;

namespace HappyTravel.Edo.Api.Models.Bookings
{
    public readonly struct BookingVoucherData
    {
        public BookingVoucherData(string agentName, int bookingId, in AccommodationInfo accommodation, int nightCount,
            in DateTime checkInDate, in DateTime checkOutDate, DateTime? deadlineDate, 
            string mainPassengerName, string referenceCode, string bannerUrl, string logoUrl, List<RoomInfo> roomDetails)
        {
            AgentName = agentName;
            Accommodation = accommodation;
            NightCount = nightCount;
            BookingId = bookingId;
            CheckInDate = checkInDate;
            CheckOutDate = checkOutDate;
            DeadlineDate = deadlineDate;
            MainPassengerName = mainPassengerName;
            ReferenceCode = referenceCode;
            BannerUrl = bannerUrl;
            LogoUrl = logoUrl;
            RoomDetails = roomDetails;
        }


        public int BookingId { get; }
        public string AgentName { get; }
        public AccommodationInfo Accommodation {get;}
        public int NightCount { get; }
        public DateTime CheckInDate { get; }
        public DateTime CheckOutDate { get; }
        public DateTime? DeadlineDate { get; }
        public string MainPassengerName { get; }
        public string ReferenceCode { get; }
        public string BannerUrl { get; }
        public string LogoUrl { get; }
        public List<RoomInfo> RoomDetails { get; }


        public readonly struct AccommodationInfo
        {
            public AccommodationInfo(string name, in SlimLocationInfo locationInfo, in ContactInfo contactInfo)
            {
                ContactInfo = contactInfo;
                Location = locationInfo;
                Name = name;
            }


            public ContactInfo ContactInfo { get; }
            public SlimLocationInfo Location { get; }
            public string Name { get; }
        }
        
        public readonly struct RoomInfo
        {
            public RoomInfo(RoomTypes type, BoardBasisTypes boardBasis, string mealPlan,
                DateTime? deadlineDate, string contractDescription, List<Passenger> passengers,
                List<KeyValuePair<string, string>> remarks, string supplierRoomReferenceCode)
            {
                Type = type;
                BoardBasis = boardBasis;
                MealPlan = mealPlan;
                DeadlineDate = deadlineDate;
                ContractDescription = contractDescription;
                Passengers = passengers;
                Remarks = remarks;
                SupplierRoomReferenceCode = supplierRoomReferenceCode;
            }
            
            public RoomTypes Type { get; }
            public BoardBasisTypes BoardBasis { get; }
            public string MealPlan { get; }
            public DateTime? DeadlineDate { get; }
            public string ContractDescription { get; }
            public List<Passenger> Passengers { get; }
            public List<KeyValuePair<string, string>> Remarks { get; }
            public string SupplierRoomReferenceCode { get; }
        }
    }
}