namespace HappyTravel.Edo.Api.AdministratorServices.Models
{
    public readonly struct CreateDiscountRequest
    {
        public decimal DiscountPercent { get; init; }
        public string Description { get; init; }
        public int TargetMarkupId { get; init; }
    }
}