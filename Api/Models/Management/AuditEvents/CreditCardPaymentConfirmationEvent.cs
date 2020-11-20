namespace HappyTravel.Edo.Api.Models.Management.AuditEvents
{
    public readonly struct CreditCardPaymentConfirmationEvent
    {
        public CreditCardPaymentConfirmationEvent(int id, string referenceCode)
        {
            Id = id;
            ReferenceCode = referenceCode;
        }

        public int Id { get; }
        public string ReferenceCode { get; }
    }
}