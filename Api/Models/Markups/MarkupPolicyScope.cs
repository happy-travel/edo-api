using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Common.Enums.Markup;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Markups
{
    public readonly struct MarkupPolicyScope
    {
        [JsonConstructor]
        public MarkupPolicyScope(MarkupPolicyScopeType type, int? counterpartyId = null, int? agencyId = null, int? agentId = null)
        {
            Type = type;
            CounterpartyId = counterpartyId;
            AgencyId = agencyId;
            AgentId = agentId;
        }


        public Result Validate()
        {
            return GenericValidator<MarkupPolicyScope>.Validate(v =>
            {
                v.RuleFor(s => s.CounterpartyId).NotEmpty().When(t => t.Type == MarkupPolicyScopeType.Counterparty)
                    .WithMessage("CounterpartyId is required");
                v.RuleFor(s => s.AgencyId).NotEmpty().When(t => t.Type == MarkupPolicyScopeType.Agency)
                    .WithMessage("AgencyId is required");
                v.RuleFor(s => s.AgencyId).NotEmpty().When(t => t.Type == MarkupPolicyScopeType.Agent)
                    .WithMessage("AgencyId is required");
                v.RuleFor(s => s.AgentId).NotEmpty().When(t => t.Type == MarkupPolicyScopeType.Agent)
                    .WithMessage("AgentId is required");

                v.RuleFor(s => s.CounterpartyId).Empty().When(t => t.Type != MarkupPolicyScopeType.Counterparty)
                    .WithMessage("CounterpartyId must be empty");
                v.RuleFor(s => s.AgencyId).Empty()
                    .When(t => t.Type != MarkupPolicyScopeType.Agency && t.Type != MarkupPolicyScopeType.Agent)
                    .WithMessage("AgencyId must be empty");
                v.RuleFor(s => s.AgentId).Empty().When(t => t.Type != MarkupPolicyScopeType.Agent)
                    .WithMessage("AgentId must be empty");
            }, this);
        }


        public void Deconstruct(out MarkupPolicyScopeType type, out int? counterpartyId, out int? agencyId, out int? agentId)
        {
            type = Type;
            counterpartyId = CounterpartyId;
            agencyId = AgencyId;
            agentId = AgentId;
        }


        /// <summary>
        ///     Scope type.
        /// </summary>
        public MarkupPolicyScopeType Type { get; }

        /// <summary>
        ///     Counterparty id for counterparty scope type
        /// </summary>
        public int? CounterpartyId { get; }

        /// <summary>
        ///     Agency id for agency scope type or agent scope type
        /// </summary>
        public int? AgencyId { get; }

        /// <summary>
        ///     Agent id for agent scope type
        /// </summary>
        public int? AgentId { get; }
    }
}