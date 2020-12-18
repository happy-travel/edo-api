using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct RoomContractSet
    {
        [JsonConstructor]
        public RoomContractSet(Guid id, in Rate rate, Deadline deadline, List<RoomContract> rooms,
            bool isAdvancePurchaseRate, Suppliers? supplier)
        {
            Id = id;
            Rate = rate;
            Deadline = deadline;
            Rooms = rooms ?? new List<RoomContract>(0);
            IsAdvancePurchaseRate = isAdvancePurchaseRate;
            Supplier = supplier;
        }
        
        /// <summary>
        ///     The set ID.
        /// </summary>
        public Guid Id { get; }
        
        /// <summary>
        ///     The total set price.
        /// </summary>
        public Rate Rate { get; }
        
        /// <summary>
        /// Deadline information
        /// </summary>
        public Deadline Deadline { get; }
        
        /// <summary>
        /// Is advanced purchase rate (Non-refundable)
        /// </summary>
        public bool IsAdvancePurchaseRate { get; }
        
        /// <summary>
        ///     The list of room contracts within a set.
        /// </summary>
        public List<RoomContract> Rooms { get; }
        public Suppliers? Supplier { get; }
    }
}