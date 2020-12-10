using System;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Payments
{
    public readonly struct PaymentHistoryData
    {
        public PaymentHistoryData(in DateTime created, decimal amount, object eventData, Currencies currency, int agentId, PaymentHistoryType eventType,
            PaymentMethods paymentMethod, string accommodationName, string leadingPassenger, int bookingId, string referenceCode)
        {
            Created = created;
            Amount = amount;
            EventData = eventData;
            Currency = currency;
            AgentId = agentId;
            EventType = eventType;
            PaymentMethod = paymentMethod;
            AccommodationName = accommodationName;
            LeadingPassenger = leadingPassenger;
            BookingId = bookingId;
            ReferenceCode = referenceCode;
        }


        /// <summary>
        ///     Current operation date and time
        /// </summary>
        public DateTime Created { get; init; }

        /// <summary>
        ///     Amount of money for the current operation
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        ///     Description of the current operation
        /// </summary>
        public object EventData { get; init; }

        /// <summary>
        ///     Money currency
        /// </summary>
        public Currencies Currency { get; init; }

        /// <summary>
        ///     Agent Id
        /// </summary>
        public int AgentId { get; init; }

        /// <summary>
        ///     Current operation type
        /// </summary>
        public PaymentHistoryType EventType { get; init; }

        /// <summary>
        ///     Payment method
        /// </summary>
        public PaymentMethods PaymentMethod { get; init; }

        /// <summary>
        /// Accommodation title
        /// </summary>
        public string AccommodationName { get; init; }
        
        /// <summary>
        /// Leading passenger name
        /// </summary>
        public string LeadingPassenger { get; init; }

        /// <summary>
        /// Id of the booking
        /// </summary>
        public int BookingId { get; init; }

        /// <summary>
        /// Booking reference code
        /// </summary>
        public string ReferenceCode { get; init; }
    }
}