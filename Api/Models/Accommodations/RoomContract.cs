using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Accommodations
{
    public readonly struct RoomContract
    {
        [JsonConstructor]
        public RoomContract(BoardBasisTypes boardBasis, string mealPlan, int contractTypeCode, bool isAvailableImmediately,
            bool isDynamic, string contractDescription, List<KeyValuePair<string, string>> remarks, List<DailyRate> dailyRoomRates,
            Rate rate, int adultsNumber, List<int> childrenAges, RoomTypes type, bool isExtraBedNeeded, Deadline deadline,
            bool isAdvancePurchaseRate)
        {
            BoardBasis = boardBasis;
            MealPlan = mealPlan;
            ContractTypeCode = contractTypeCode;
            IsAvailableImmediately = isAvailableImmediately;
            IsDynamic = isDynamic;
            ContractDescription = contractDescription;
            Rate = rate;
            Remarks = remarks;
            AdultsNumber = adultsNumber;
            ChildrenAges = childrenAges;
            IsExtraBedNeeded = isExtraBedNeeded;
            Deadline = deadline;
            IsAdvancePurchaseRate = isAdvancePurchaseRate;
            DailyRoomRates = dailyRoomRates;
            Type = type;
        }


        /// <summary>
        ///     The board basis of a contract.
        /// </summary>
        public BoardBasisTypes BoardBasis { get; }

        /// <summary>
        ///     The textual description of a board basis.
        /// </summary>
        public string MealPlan { get; }

        /// <summary>
        ///     The numerical code of a contract type.
        /// </summary>
        public int ContractTypeCode { get; }

        public bool IsAvailableImmediately { get; }

        /// <summary>
        ///     Indicates if a contract a dynamic offer.
        /// </summary>
        public bool IsDynamic { get; }

        /// <summary>
        ///     The textual contract description i.e. "Pool View Suite", "Ocean Club Room", or "Pioneer Cabin".
        /// </summary>
        public string ContractDescription { get; }

        /// <summary>
        ///     The total contract price.
        /// </summary>
        public Rate Rate { get; }

        /// <summary>
        ///     Contract remarks.
        /// </summary>
        public List<KeyValuePair<string, string>> Remarks { get; }

        /// <summary>
        ///     Required. Number of adult passengers.
        /// </summary>
        [Required]
        public int AdultsNumber { get; }

        /// <summary>
        ///     Ages of each child.
        /// </summary>
        public List<int> ChildrenAges { get; }

        /// <summary>
        ///     Indicates if extra child bed needed.
        /// </summary>
        public bool IsExtraBedNeeded { get; }

        /// <summary>
        ///     Deadline and cancellation information.
        ///     <b>Null considers as an unknown deadline for first search steps, and as an empty deadline for the evaluation step.</b>
        /// </summary>
        public Deadline Deadline { get; }

        /// <summary>
        ///     Indicates if a contract is an advance purchase.
        /// </summary>
        public bool IsAdvancePurchaseRate { get; }

        /// <summary>
        ///     List of room prices on daily basis
        /// </summary>
        public List<DailyRate> DailyRoomRates { get; }

        /// <summary>
        ///     Desirable room type.
        /// </summary>
        public RoomTypes Type { get; }
    }
}