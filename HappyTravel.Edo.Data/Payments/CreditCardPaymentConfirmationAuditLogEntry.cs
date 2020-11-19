using System;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Data.Payments
{
    public class CreditCardPaymentConfirmationAuditLogEntry
    {
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public int UserId { get; set; }
        public UserTypes UserType { get; set; }
        public string ReferenceCode { get; set; }
    }
}