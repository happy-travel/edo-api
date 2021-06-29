using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Services.Management;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters
{
    public class AdministratorPermissionsAuthorizationHandler : AuthorizationHandler<AdministratorPermissionsAuthorizationRequirement>
    {
        public AdministratorPermissionsAuthorizationHandler(IAdministratorContext administratorContext, 
            IExternalAdminContext externalAdminContext,
            ILogger<AdministratorPermissionsAuthorizationHandler> logger)
        {
            _administratorContext = administratorContext;
            _externalAdminContext = externalAdminContext;
            _logger = logger;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministratorPermissionsAuthorizationRequirement requirement)
        {
            if (_externalAdminContext.HasPermission(requirement.AdministratorPermissions))
            {
                _logger.LogExternalAdministratorAuthorizationSuccess();
                context.Succeed(requirement);
            }
            else if (await _administratorContext.HasPermission(requirement.AdministratorPermissions))
            {
                var adminEmail = (await _administratorContext.GetCurrent()).Value.Email;
                _logger.LogInternalAdministratorAuthorizationSuccess(adminEmail);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogAdministratorAuthorizationFailure();
                context.Fail();
            }
        }


        private readonly IAdministratorContext _administratorContext;
        private readonly IExternalAdminContext _externalAdminContext;
        private readonly ILogger<AdministratorPermissionsAuthorizationHandler> _logger;
    }
}