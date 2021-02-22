using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Markups.Templates;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums.Markup;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/markups")]
    [Produces("application/json")]
    public class MarkupsController : BaseController
    {
        public MarkupsController(IAdminMarkupPolicyManager policyManager,
            IMarkupPolicyTemplateService policyTemplateService)
        {
            _policyManager = policyManager;
            _policyTemplateService = policyTemplateService;
        }


        /// <summary>
        ///     Creates markup policy.
        /// </summary>
        /// <param name="policyData">Policy data.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> AddPolicy([FromBody] MarkupPolicyData policyData)
        {
            var (_, isFailure, error) = await _policyManager.Add(policyData);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Deletes policy.
        /// </summary>
        /// <param name="id">Id of the policy to delete.</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> RemovePolicy(int id)
        {
            var (_, isFailure, error) = await _policyManager.Remove(id);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Updates policy settings.
        /// </summary>
        /// <param name="id">Id of the policy.</param>
        /// <param name="policySettings">Updated settings.</param>
        /// <returns></returns>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> ModifyPolicy(int id, [FromBody] MarkupPolicySettings policySettings)
        {
            var (_, isFailure, error) = await _policyManager.Modify(id, policySettings);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Gets policies for specified scope.
        /// </summary>
        /// <returns>Policies.</returns>
        [HttpGet("{scopeType}")]
        [ProducesResponseType(typeof(List<MarkupPolicyData>), (int) HttpStatusCode.NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> GetPolicies(MarkupPolicyScopeType scopeType, [FromQuery] int? counterpartyId, [FromQuery] int? agencyId,
            [FromQuery] int? agentId)
        {
            var scope = scopeType switch
            {
                MarkupPolicyScopeType.Global => new MarkupPolicyScope(scopeType),
                MarkupPolicyScopeType.Counterparty => new MarkupPolicyScope(scopeType, counterpartyId: counterpartyId),
                MarkupPolicyScopeType.Agency => new MarkupPolicyScope(scopeType, agencyId: agencyId),
                MarkupPolicyScopeType.Agent => new MarkupPolicyScope(scopeType, agencyId: agencyId, agentId: agentId),
                _ => default,
            };

            var (_, isValidationFailure, validationError) = scope.Validate();
            if (isValidationFailure)
                return BadRequest(ProblemDetailsBuilder.Build(validationError));

            return Ok(await _policyManager.Get(scope));
        }


        /// <summary>
        ///     Gets policy templates.
        /// </summary>
        /// <returns>Policy templates.</returns>
        [HttpGet("templates")]
        [ProducesResponseType(typeof(ReadOnlyCollection<MarkupPolicyTemplate>), (int) HttpStatusCode.OK)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public IActionResult GetPolicyTemplates() => Ok(_policyTemplateService.Get());


        private readonly IAdminMarkupPolicyManager _policyManager;
        private readonly IMarkupPolicyTemplateService _policyTemplateService;
    }
}