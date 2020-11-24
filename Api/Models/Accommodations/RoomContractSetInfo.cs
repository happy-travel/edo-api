using System;
using System.Collections.Generic;
using System.Linq;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct RoomContractSetInfo
    {
        private RoomContractSetInfo(
            Guid id,
            in Rate rate,
            Deadline deadline,
            List<RoomContract> rooms,
            bool isAdvancedPurchaseRate,
            Suppliers? supplier)
        {
            Id = id;
            Rate = rate;
            Deadline = deadline;
            Rooms = rooms ?? new List<RoomContract>(0);
            IsAdvancedPurchaseRate = isAdvancedPurchaseRate;
            Supplier = supplier;
        }
        
        
        public static RoomContractSetInfo FromRoomContractSet(in RoomContractSet roomContractSet, Suppliers? supplier)
        {
            var deadline = roomContractSet.Deadline;
            var policies = deadline.Policies
                .Select(p => new Data.Booking.CancellationPolicy(p.FromDate, p.Percentage))
                .ToList();
            
            var rate = new Rate(roomContractSet.Rate.FinalPrice, roomContractSet.Rate.Gross,
                roomContractSet.Rate.Discounts);
            
            return new RoomContractSetInfo(roomContractSet.Id,
                rate,
                new Deadline(deadline.Date, policies, deadline.Remarks), 
                roomContractSet.RoomContracts,
                roomContractSet.IsAdvancePurchaseRate,
                supplier);
        }
        
        
        public Guid Id { get; }
        public Rate Rate { get; }
        public Deadline Deadline { get; }
        public List<RoomContract> Rooms { get; }
        public bool IsAdvancedPurchaseRate { get; }
        public Suppliers? Supplier { get; }
    }
}