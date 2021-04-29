using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Common.Enums.Markup;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/counterparties/{counterpartyId:int}/markups")]
    [Produces("application/json")]
    public class CounterpartyMarkupsController : BaseController
    {
        public CounterpartyMarkupsController(IAdminMarkupPolicyManager policyManager)
        {
            _policyManager = policyManager;
        }
        
        
        /// <summary>
        /// Gets markup policies for a counterparty.
        /// </summary>
        /// <param name="counterpartyId">Counterparty id</param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(List<MarkupInfo>), StatusCodes.Status200OK)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> GetCounterpartyPolicies([FromRoute] int counterpartyId)
        {
            return Ok(await _policyManager.GetMarkupsForCounterparty(counterpartyId));
        }
        
        
        /// <summary>
        /// Creates counterparty markup policy.
        /// </summary>
        /// <param name="counterpartyId">Counterparty id</param>
        /// <param name="settings">Markup settings</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> AddPolicy([FromRoute] int counterpartyId, [FromBody] MarkupPolicySettings settings)
        {
            var (_, isFailure, error) = await _policyManager.AddCounterpartyPolicy(counterpartyId, settings);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        /// <summary>
        ///     Deletes counterparty policy.
        /// </summary>
        /// <param name="counterpartyId">Counterparty id</param>
        /// <param name="policyId">Id of the policy to delete.</param>
        /// <returns></returns>
        [HttpDelete("{policyId:int}")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> RemovePolicy([FromRoute] int counterpartyId, [FromRoute] int policyId)
        {
            var (_, isFailure, error) = await _policyManager.RemoveFromCounterparty(policyId, counterpartyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        /// <summary>
        ///     Updates policy settings.
        /// </summary>
        /// <param name="counterpartyId">Counterparty id</param>
        /// <param name="policyId">Id of the policy.</param>
        /// <param name="policySettings">Updated settings.</param>
        /// <returns></returns>
        [HttpPut("{policyId:int}")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> ModifyPolicy(int counterpartyId, int policyId, [FromBody] MarkupPolicySettings policySettings)
        {
            var (_, isFailure, error) = await _policyManager.ModifyCounterpartyPolicy(policyId, counterpartyId, policySettings);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        private readonly IAdminMarkupPolicyManager _policyManager;
    }
}