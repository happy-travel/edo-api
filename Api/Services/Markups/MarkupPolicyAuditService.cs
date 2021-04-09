using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Markup;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public class MarkupPolicyAuditService : IMarkupPolicyAuditService
    {
        public MarkupPolicyAuditService(EdoContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task Write<TEventData>(MarkupPolicyEventType eventType, TEventData eventData, ApiCaller apiCaller)
        {
            var logEntry = new MarkupPolicyAuditLogEntry
            {
                Created = _dateTimeProvider.UtcNow(),
                Type = eventType,
                UserId = apiCaller.Id,
                ApiCallerType = apiCaller.Type,
                EventData = JsonConvert.SerializeObject(eventData)
            };

            _context.MarkupPolicyAuditLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}
