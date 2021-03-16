namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public readonly struct PaymentInformation
    {
        public string Pan { get; init; }
        public string Expiry { get; init; }
        public string Cvv { get; init; }
        public string CardholderName { get; init; }
    }
}