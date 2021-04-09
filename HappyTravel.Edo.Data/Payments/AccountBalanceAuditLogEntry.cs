using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Payments
{
    public class AccountBalanceAuditLogEntry
    {
        public int Id { get; set; }
        public AccountEventType Type { get; set; }
        public DateTime Created { get; set; }
        public int UserId { get; set; }
        public ApiCallerTypes ApiCallerType { get; set; }
        public int AccountId { get; set; }
        public decimal Amount { get; set; }
        public string EventData { get; set; }
        public string ReferenceCode { get; set; }
    }
}
