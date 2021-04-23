using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using System;

namespace HappyTravel.Edo.Api.Models.Counterparties
{
    public readonly struct FullCounterpartyAccountInfo
    {
        /// <summary>
        /// Account id
        /// </summary>
        public int Id { get; init; }
        
        /// <summary>
        /// Currency
        /// </summary>
        public Currencies Currency { get; init; }
        
        /// <summary>
        /// Balance
        /// </summary>
        public MoneyAmount Balance { get; init; }

        /// <summary>
        /// Date and time of adding account
        /// </summary>
        public DateTime Created { get; init; }

        /// <summary>
        /// Is the account active
        /// </summary>
        public bool IsActive { get; init; }
    }
}