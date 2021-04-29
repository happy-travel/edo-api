using System;

namespace HappyTravel.Edo.Notifications.Enums
{
    [Flags]
    public enum ProtocolTypes
    {
        WebSocket = 1,
        Email = 2
    }
}