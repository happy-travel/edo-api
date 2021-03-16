namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public readonly struct Amount
    {
        public string CurrencyCode { get; init; }
        public decimal Value { get; init; }
    }
}