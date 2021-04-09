using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HappyTravel.Edo.Common.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApiCallerTypes
    {
        NotSpecified = 0,
        Admin = 1,
        Agent = 2,
        ServiceAccount = 3,
        InternalServiceAccount = 4,
        Supplier = 5
    }
}
