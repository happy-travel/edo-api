using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments
{
    public readonly struct AccountPaymentInfo
    {
        [JsonConstructor]
        public AccountPaymentInfo(string customerIp)
        {
            CustomerIp = customerIp;
        }


        public string CustomerIp { get; }
    }
}