using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class AccountBalanceAuditService : IAccountBalanceAuditService
    {
        public AccountBalanceAuditService(EdoContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task Write<TEventData>(AccountEventType eventType, int accountId, decimal amount, UserInfo user, TEventData eventData, string referenceCode)
        {
            var logEntry = new AccountBalanceAuditLogEntry
            {
                Created = _dateTimeProvider.UtcNow(),
                Type = eventType,
                AccountId = accountId,
                Amount = amount,
                UserId = user.Id,
                UserType = user.Type,
                EventData = JsonConvert.SerializeObject(eventData),
                ReferenceCode = referenceCode
            };

            _context.AccountBalanceAuditLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}