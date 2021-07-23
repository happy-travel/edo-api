namespace HappyTravel.Edo.Api.Models.Storage
{
    public record CachedDiscount
    {
        public string? Description { get; init; }
        public double Percent { get; init; }
    }
}