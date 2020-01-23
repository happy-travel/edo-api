using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CreditCardPaymentStatuses
    {
        Created = 0,
        Success = 1,
        Secure3d  = 2,
        Failed  = 3
    }
}
