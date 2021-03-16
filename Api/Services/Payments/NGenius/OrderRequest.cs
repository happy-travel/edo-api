namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public readonly struct OrderRequest
    {
        public string Action { get; init; }
        public Amount Amount { get; init; }
        public string EmailAddress { get; init; }
        public BillingAddress BillingAddress { get; init; }
        public string Language { get; init; }
        public string MerchantOrderReference { get; init; }
    }
}