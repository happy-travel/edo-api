using HappyTravel.Edo.Notifications.Enums;
using System.Collections.Generic;
using System.Text.Json;

namespace HappyTravel.Edo.Notifications.Models
{
    public readonly struct Notification
    {
        public int UserId { get; init; }
        public int? AgencyId { get; init; }
        public ReceiverTypes Receiver { get; init; }
        public JsonDocument Message { get; init; }
        public NotificationTypes Type { get; init; }
        public Dictionary<ProtocolTypes, object> SendingSettings { get; init; }
    }
}