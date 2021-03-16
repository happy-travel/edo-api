namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public readonly struct AuthResponse
    {
        public string AccessToken { get; init; }
        public int ExpiresIn { get; init; }
    }
}