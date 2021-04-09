using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;

namespace HappyTravel.Edo.Api.Services.Payments.Offline
{
    public class OfflinePaymentAuditService : IOfflinePaymentAuditService
    {
        public OfflinePaymentAuditService(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task Write(ApiCaller apiCaller, string referenceCode)
        {
            var logEntry = new OfflinePaymentAuditLogEntry
            {
                Created = _dateTimeProvider.UtcNow(),
                UserId = apiCaller.Id,
                ApiCallerType = apiCaller.Type,
                ReferenceCode = referenceCode
            };

            _context.OfflinePaymentAuditLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}
