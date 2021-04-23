using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Counterparties
{
    public readonly struct CounterpartyAccountRequest
    {
        [JsonConstructor]
        public CounterpartyAccountRequest(bool isActive)
        {
            IsActive = isActive;
        }


        /// <summary>
        /// Is the account active
        /// </summary>
        public bool IsActive { get; }
    }
}
