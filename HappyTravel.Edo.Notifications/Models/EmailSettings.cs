using System.Collections.Generic;

namespace HappyTravel.Edo.Notifications.Models
{
    public readonly struct EmailSettings : ISendingSettings
    {
        public List<string> Emails { get; init; }
        public string TemplateId { get; init; }
    }
}