using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Markup
{
    public class MarkupPolicyAuditLogEntry
    {
        public int Id { get; set; }
        public MarkupPolicyEventType Type { get; set; }
        public DateTime Created { get; set; }
        public int UserId { get; set; }
        public ApiCallerTypes ApiCallerType { get; set; }
        public string EventData { get; set; }
    }
}
