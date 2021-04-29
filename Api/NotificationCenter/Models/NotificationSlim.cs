using HappyTravel.Edo.Notifications.Enums;
using System;
using System.Text.Json;

namespace HappyTravel.Edo.Api.NotificationCenter.Models
{
    public readonly struct SlimNotification
    {
        public int Id { get; init; }
        public ReceiverTypes Receiver { get; init; }
        public int UserId { get; init; }
        public JsonDocument Message { get; init; }
        public NotificationTypes Type { get; init; }
        public DateTime Created { get; init; }
        public bool IsRead { get; init; }
    }
}