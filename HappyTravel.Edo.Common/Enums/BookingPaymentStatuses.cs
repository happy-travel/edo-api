using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BookingPaymentStatuses
    {
        NotPaid = 0,
        MoneyFrozen = 1,
        Paid = 2,
        Refunded = 3,
        Cancelled = 4
    }
}
