using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Markups;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/global-markups")]
    [Produces("application/json")]
    public class GlobalMarkupsController : BaseController
    {
        public GlobalMarkupsController(IAdminMarkupPolicyManager policyManager)
        {
            _policyManager = policyManager;
        }
        
        
        /// <summary>
        /// Gets global markup policies
        /// </summary>
        /// <returns>List of global markups</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(List<MarkupInfo>), StatusCodes.Status200OK)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> GetPolicies()
        {
            return Ok(await _policyManager.GetGlobalPolicies());
        }
        
        
        /// <summary>
        /// Creates global markup policy.
        /// </summary>
        /// <param name="settings">Markup settings</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> AddPolicy([FromBody] MarkupPolicySettings settings)
        {
            var (_, isFailure, error) = await _policyManager.AddGlobalPolicy(settings);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        /// <summary>
        ///     Deletes global policy.
        /// </summary>
        /// <param name="policyId">Id of the policy to delete.</param>
        /// <returns></returns>
        [HttpDelete("{policyId:int}")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> RemovePolicy([FromRoute] int policyId)
        {
            var (_, isFailure, error) = await _policyManager.RemoveGlobalPolicy(policyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        /// <summary>
        ///     Updates policy settings.
        /// </summary>
        /// <param name="policyId">Id of the policy.</param>
        /// <param name="policySettings">Updated settings.</param>
        /// <returns></returns>
        [HttpPut("{policyId:int}")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> ModifyPolicy(int policyId, [FromBody] MarkupPolicySettings policySettings)
        {
            var (_, isFailure, error) = await _policyManager.ModifyGlobalPolicy(policyId, policySettings);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        private readonly IAdminMarkupPolicyManager _policyManager;
    }
}