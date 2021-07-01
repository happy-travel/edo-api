using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Administrators;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums.Administrators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/administrators")]
    [Produces("application/json")]
    public class AdministratorsController : BaseController
    {
        public AdministratorsController(IAdministratorContext administratorContext,
            IAdministratorRolesAssignmentService administratorRolesAssignmentService)
        {
            _administratorContext = administratorContext;
            _administratorRolesAssignmentService = administratorRolesAssignmentService;
        }

        /// <summary>
        ///     Gets current administrator information
        /// </summary>
        /// <returns>Current administrator information.</returns>
        [HttpGet("current")]
        [ProducesResponseType(typeof(AdministratorInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetCurrent()
        {
            var (_, isFailure, administrator, _) = await _administratorContext.GetCurrent();

            return isFailure
                ? NoContent()
                : Ok(new AdministratorInfo(administrator.Id,
                    administrator.FirstName,
                    administrator.LastName,
                    administrator.Position));
        }


        /// <summary>
        ///     Assigns new set of roles to an administrator
        /// </summary>
        [HttpPut("{administratorId:int}/roles")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [AdministratorPermissions(AdministratorPermissions.AdministratorManagement)]
        public async Task<IActionResult> SetRoles([FromRoute] int administratorId, [FromBody] List<int> roleIds)
        {
            var (_, isGetAdminFailure, administrator, getAdminError) = await _administratorContext.GetCurrent();

            if(isGetAdminFailure)
                return BadRequest(ProblemDetailsBuilder.Build(getAdminError));

            return OkOrBadRequest(await _administratorRolesAssignmentService.SetAdministratorRoles(administratorId, roleIds, administrator));
        }


        private readonly IAdministratorContext _administratorContext;
        private readonly IAdministratorRolesAssignmentService _administratorRolesAssignmentService;
    }
}