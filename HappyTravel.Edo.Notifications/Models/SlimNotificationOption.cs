using HappyTravel.Edo.Notifications.Enums;

namespace HappyTravel.Edo.Notifications.Models
{
    public readonly struct SlimNotificationOptions
    {
        public ProtocolTypes EnabledProtocols { get; init; }
        public bool IsMandatory { get; init; }
    }
}