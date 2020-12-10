using System;
using System.Linq;
using System.Linq.Expressions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.General.Enums;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PaymentHistoryService : IPaymentHistoryService
    {
        public PaymentHistoryService(EdoContext edoContext)
        {
            _edoContext = edoContext;
        }


        public IQueryable<PaymentHistoryData> GetAgentHistory(AgentContext agent)
        {
            return GetData(agent.AgentId, booking => booking.AgentId == agent.AgentId);
        }


        public IQueryable<PaymentHistoryData> GetAgencyHistory(AgentContext agent)
        {
            return GetData(agent.AgentId, booking => booking.AgencyId == agent.AgencyId);
        }


        private IQueryable<PaymentHistoryData> GetData(int agentId, Expression<Func<Booking, bool>> filterExpression)
        {
            var bookings = _edoContext.Bookings.Where(filterExpression);

            return from booking in bookings
                join accountAuditLogEntry in _edoContext.AccountBalanceAuditLogs
                    on booking.ReferenceCode equals accountAuditLogEntry.ReferenceCode into accountAuditLogEntries
                from accountAuditLogEntry in accountAuditLogEntries.DefaultIfEmpty()
                join cardAuditLogEntry in _edoContext.CreditCardAuditLogs
                    on booking.ReferenceCode equals cardAuditLogEntry.ReferenceCode into cardAuditLogEntries
                from cardAuditLogEntry in cardAuditLogEntries.DefaultIfEmpty()
                where
                    accountAuditLogEntry.UserId == agentId &&
                    accountAuditLogEntry.UserType == UserTypes.Agent ||
                    cardAuditLogEntry.UserId == agentId &&
                    cardAuditLogEntry.UserType == UserTypes.Agent
                let isAccountLogEntry = accountAuditLogEntry != null && cardAuditLogEntry == null
                let eventType = isAccountLogEntry ? ToPaymentHistoryType(accountAuditLogEntry.Type) : ToPaymentHistoryType(cardAuditLogEntry.Type)
                select new PaymentHistoryData
                {
                    Created = isAccountLogEntry ? accountAuditLogEntry.Created : cardAuditLogEntry.Created,
                    Amount = isAccountLogEntry ? accountAuditLogEntry.Amount : cardAuditLogEntry.Amount,
                    EventData = JObject.Parse(isAccountLogEntry ? accountAuditLogEntry.EventData : cardAuditLogEntry.EventData),
                    Currency = booking.Currency,
                    AgentId = booking.AgentId,
                    EventType = eventType,
                    PaymentMethod = isAccountLogEntry ? PaymentMethods.BankTransfer : PaymentMethods.CreditCard,
                    AccommodationName = booking.AccommodationName,
                    LeadingPassenger = booking.MainPassengerName,
                    BookingId = booking.Id,
                    ReferenceCode = booking.ReferenceCode
                };
        }


        private static PaymentHistoryType ToPaymentHistoryType(AccountEventType type)
        {
            return type switch
            {
                AccountEventType.None => PaymentHistoryType.None,
                AccountEventType.Add => PaymentHistoryType.Add,
                AccountEventType.Charge => PaymentHistoryType.Charge,
                AccountEventType.Authorize => PaymentHistoryType.Authorize,
                AccountEventType.Capture => PaymentHistoryType.Capture,
                AccountEventType.Void => PaymentHistoryType.Void,
                AccountEventType.Refund => PaymentHistoryType.Refund,
                AccountEventType.CounterpartyTransferToAgency => PaymentHistoryType.Add,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }


        private static PaymentHistoryType ToPaymentHistoryType(CreditCardEventType type)
        {
            return type switch
            {
                CreditCardEventType.Authorize => PaymentHistoryType.Authorize,
                CreditCardEventType.Capture => PaymentHistoryType.Capture,
                CreditCardEventType.Void => PaymentHistoryType.None,
                CreditCardEventType.None => PaymentHistoryType.None,
                CreditCardEventType.Refund => PaymentHistoryType.Refund,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private readonly EdoContext _edoContext;
    }
}