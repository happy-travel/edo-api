using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public static class AgencyValidator
    {
        public static Result Validate(in RegistrationAgencyInfo agencyInfo)
        {
            return GenericValidator<RegistrationAgencyInfo>.Validate(v =>
            {
                v.RuleFor(c => c.Name).NotEmpty();
                v.RuleFor(c => c.Address).NotEmpty();
                v.RuleFor(c => c.City).NotEmpty();
                v.RuleFor(c => c.Phone).NotEmpty().Matches(@"^[0-9]{3,30}$");
                v.RuleFor(c => c.Fax).Matches(@"^[0-9]{3,30}$").When(i => !string.IsNullOrWhiteSpace(i.Fax));
            }, agencyInfo);
        }
    }
}