using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Notifications.Enums;

namespace HappyTravel.Edo.Data.Notifications
{
    public class NotificationOptions : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ApiCallerTypes UserType { get; set; }
        public int? AgencyId { get; set; }
        public NotificationTypes Type { get; set; }
        public ProtocolTypes EnabledProtocols { get; set; }
        public bool IsMandatory { get; set; }
    }
}