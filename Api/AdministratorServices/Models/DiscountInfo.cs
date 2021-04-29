namespace HappyTravel.Edo.Api.AdministratorServices.Models
{
    public readonly struct DiscountInfo
    {
        public decimal DiscountPercent { get; init; }
        public string Description { get; init; }
        
        public int TargetMarkupId { get; init; }
        public string TargetPolicyDescription { get; init; }
        public bool IsActive { get; init; }
    }
}