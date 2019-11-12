using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BookingStatusCodes
    {
        /// <summary>
        ///     If you see it for more than 20 minutes (it should be a status changing very very fast to something else, like CNF
        ///     or REJ),
        ///     that something went wrong. A manual intervention is required.
        /// </summary>
        InternalProcessing = 0,
        WaitingForResponse = 1,
        Pending = 2,
        Confirmed = 3,
        Cancelled = 4,
        Rejected = 5,
        Invalid = 6
    }
}
