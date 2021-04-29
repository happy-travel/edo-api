using System;
using System.Text.Json;
using HappyTravel.Edo.Notifications.Enums;

namespace HappyTravel.Edo.Data.Notifications
{
    public class Notification : IEntity, IDisposable
    {
        public int Id { get; set; }
        public ReceiverTypes Receiver { get; set; }
        public int UserId { get; set; }
        public int? AgencyId { get; set; }
        public JsonDocument Message { get; set; }
        public NotificationTypes Type { get; set; }
        public JsonDocument SendingSettings { get; set; }
        public DateTime Created { get; set; }
        public bool IsRead { get; set; }


        public void Dispose()
        {
            Message?.Dispose();
            SendingSettings?.Dispose();
        }
    }
}