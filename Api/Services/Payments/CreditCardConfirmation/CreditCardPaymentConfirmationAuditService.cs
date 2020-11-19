using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCardConfirmation
{
    public class CreditCardPaymentConfirmationAuditService : ICreditCardPaymentConfirmationAuditService
    {
        public CreditCardPaymentConfirmationAuditService(EdoContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task Write(UserInfo user, string referenceCode)
        {
            var logEntry = new CreditCardPaymentConfirmationAuditLogEntry
            {
                Created = _dateTimeProvider.UtcNow(),
                UserId = user.Id,
                UserType = user.Type,
                ReferenceCode = referenceCode
            };

            _context.CreditCardPaymentConfirmationAuditLogs.Add(logEntry);
            await _context.SaveChangesAsync();
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}