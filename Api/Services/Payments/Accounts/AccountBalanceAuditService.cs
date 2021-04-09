using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public class AccountBalanceAuditService : IAccountBalanceAuditService
    {
        public AccountBalanceAuditService(EdoContext context,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }


        public async Task Write<TEventData>(AccountEventType eventType, int accountId, decimal amount, ApiCaller apiCaller, TEventData eventData, string referenceCode)
        {
            var logEntry = new AccountBalanceAuditLogEntry
            {
                Created = _dateTimeProvider.UtcNow(),
                Type = eventType,
                AccountId = accountId,
                Amount = amount,
                UserId = apiCaller.Id,
                ApiCallerType = apiCaller.Type,
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