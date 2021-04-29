namespace HappyTravel.Edo.Api.AdministratorServices.Models
{
    public readonly struct EditDiscountRequest
    {
        public decimal DiscountPercent { get; init; }
        public string Description { get; init; }
    }
}