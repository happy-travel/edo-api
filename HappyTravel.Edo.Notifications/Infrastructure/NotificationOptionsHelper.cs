using CSharpFunctionalExtensions;
using HappyTravel.Edo.Notifications.Enums;
using HappyTravel.Edo.Notifications.Models;
using System.Collections.Generic;

namespace HappyTravel.Edo.Notifications.Infrastructure
{
    public static class NotificationOptionsHelper
    {
        public static Result<SlimNotificationOptions> TryGetDefaultOptions(NotificationTypes type) 
            => _defaultOptions.TryGetValue(type, out var value)
                ? value
                : Result.Failure<SlimNotificationOptions>($"Cannot find options for type '{type}'");


        private static readonly Dictionary<NotificationTypes, SlimNotificationOptions> _defaultOptions = new()
        {
            { NotificationTypes.BookingVoucher, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = true } },
            { NotificationTypes.BookingInvoice, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = true } },
            { NotificationTypes.DeadlineApproaching, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = false } },
            { NotificationTypes.SuccessfulPaymentReceipt, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = true } },
            { NotificationTypes.BookingDuePaymentDate, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = false } },
            { NotificationTypes.BookingCancelled, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = false } },
            { NotificationTypes.BookingFinalized, new() { EnabledProtocols = ProtocolTypes.Email | ProtocolTypes.WebSocket, IsMandatory = false } },
            { NotificationTypes.BookingStatusChanged, new() { EnabledProtocols = ProtocolTypes.WebSocket, IsMandatory = false } },
        };
    }
}
