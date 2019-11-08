namespace HappyTravel.Edo.Api.Services.Payments.AuditEvents
{
    public readonly struct AccountBalanceWithReferenceCodeLogEventData
    {
        public AccountBalanceWithReferenceCodeLogEventData(string reason, string referenceCode, decimal balance, decimal creditLimit, decimal frozen)
        {
            Reason = reason;
            ReferenceCode = referenceCode;
            Balance = balance;
            CreditLimit = creditLimit;
            Frozen = frozen;
        }

        public string Reason { get; }
        public string ReferenceCode { get; }
        public decimal Balance { get; }
        public decimal CreditLimit { get; }
        public decimal Frozen { get; }
    }
}
