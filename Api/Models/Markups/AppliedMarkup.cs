using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Models.Markups
{
    public readonly struct AppliedMarkup
    {
        public AppliedMarkup(int policyId, MarkupPolicyScope scope, MoneyAmount amountChange)
        {
            PolicyId = policyId;
            Scope = scope;
            AmountChange = amountChange;
        }
        
        public int PolicyId { get; }
        public MarkupPolicyScope Scope { get; }
        public MoneyAmount AmountChange { get; }
    }
}