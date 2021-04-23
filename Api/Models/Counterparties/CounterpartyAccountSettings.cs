using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Counterparties
{
    public readonly struct CounterpartyAccountSettings
    {
        [JsonConstructor]
        public CounterpartyAccountSettings(int counterpartyId, int counterpartyAccountId, bool isActive)
        {
            CounterpartyId = counterpartyId;
            CounterpartyAccountId = counterpartyAccountId;
            IsActive = isActive;
        }


        /// <summary>
        /// Counterparty Id
        /// </summary>
        public int CounterpartyId { get; }

        /// <summary>
        /// Counterparty account Id
        /// </summary>
        public int CounterpartyAccountId { get; }

        /// <summary>
        /// Is the account active
        /// </summary>
        public bool IsActive { get; }
    }
}
