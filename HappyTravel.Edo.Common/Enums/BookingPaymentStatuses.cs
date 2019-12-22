using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BookingPaymentStatuses
    {
        NotPaid = 0,
        Authorized = 1,
        Captured = 2,
        Refunded = 3,
        Voided = 4,
        PartiallyAuthorized = 5,
    }
}
