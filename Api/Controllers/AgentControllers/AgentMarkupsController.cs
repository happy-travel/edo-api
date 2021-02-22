using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Api.Models.Markups.Templates;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AgentMarkupsController : BaseController
    {
        public AgentMarkupsController(IAgentMarkupPolicyManager policyManager,
            IMarkupPolicyTemplateService policyTemplateService,
            IAgentContextService agentContext,
            IMarkupBonusDisplayService markupBonusDisplayService)
        {
            _policyManager = policyManager;
            _policyTemplateService = policyTemplateService;
            _agentContext = agentContext;
            _markupBonusDisplayService = markupBonusDisplayService;
        }


        /// <summary>
        /// Creates markup policy.
        /// </summary>
        /// <param name="agentId">Agent id</param>
        /// <param name="settings">Markup settings</param>
        /// <returns></returns>
        [HttpPost("agency/agents/{agentId}/markups")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [InAgencyPermissions(InAgencyPermissions.MarkupManagement)]
        public async Task<IActionResult> AddPolicy([FromRoute] int agentId, [FromBody] MarkupPolicySettings settings)
        {
            var agent = await _agentContext.GetAgent();

            var (_, isFailure, error) = await _policyManager.Add(agentId, settings, agent);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Deletes policy.
        /// </summary>
        /// <param name="agentId">Agent id</param>
        /// <param name="policyId">Id of the policy to delete.</param>
        /// <returns></returns>
        [HttpDelete("agency/agents/{agentId}/markups/{policyId}")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [InAgencyPermissions(InAgencyPermissions.MarkupManagement)]
        public async Task<IActionResult> RemovePolicy([FromRoute]int agentId, [FromRoute] int policyId)
        {
            var agent = await _agentContext.GetAgent();

            var (_, isFailure, error) = await _policyManager.Remove(agentId, policyId, agent);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Updates policy settings.
        /// </summary>
        /// <param name="agentId">Agent id</param>
        /// <param name="policyId">Id of the policy.</param>
        /// <param name="policySettings">Updated settings.</param>
        /// <returns></returns>
        [HttpPut("agency/agents/{agentId}/markups/{policyId}")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [InAgencyPermissions(InAgencyPermissions.MarkupManagement)]
        public async Task<IActionResult> ModifyPolicy(int agentId, int policyId, [FromBody] MarkupPolicySettings policySettings)
        {
            var agent = await _agentContext.GetAgent();

            var (_, isFailure, error) = await _policyManager.Modify(agentId, policyId, policySettings, agent);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        /// Gets policies for specified scope.
        /// </summary>
        /// <param name="agentId">Agent id</param>
        /// <returns></returns>
        [HttpGet("agency/agents/{agentId}/markups")]
        [ProducesResponseType(typeof(List<MarkupInfo>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetPolicies(int agentId)
        {
            var agencyId = (await _agentContext.GetAgent()).AgencyId;
            return Ok(await _policyManager.Get(agentId, agencyId));
        }


        /// <summary>
        ///     Gets policy templates.
        /// </summary>
        /// <returns>Policy templates.</returns>
        [HttpGet("markup-templates")]
        [ProducesResponseType(typeof(ReadOnlyCollection<MarkupPolicyTemplate>), (int) HttpStatusCode.OK)]
        public IActionResult GetPolicyTemplates() => Ok(_policyTemplateService.Get());


        /// <summary>
        ///     Gets list of applied markups for agency
        /// </summary>
        /// <returns></returns>
        [HttpGet("agency/bonuses")]
        [InAgencyPermissions(InAgencyPermissions.ObserveMarkup)]
        [ProducesResponseType(typeof(List<Bonus>), (int) HttpStatusCode.OK)]
        [EnableQuery]
        public async Task<IActionResult> GetBonuses()
        {
            var agent = await _agentContext.GetAgent();
            return Ok(_markupBonusDisplayService.GetBonuses(agent));
        }

        
        /// <summary>
        ///     Gets summary amount of applied markups for agency
        /// </summary>
        /// <param name="filter">Filter for date range</param>
        /// <returns></returns>
        [HttpGet("agency/bonuses/sum")]
        [InAgencyPermissions(InAgencyPermissions.ObserveMarkup)]
        [ProducesResponseType(typeof(BonusSummary), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetBonusesSummary([FromQuery] BonusSummaryFilter filter)
        {
            var agent = await _agentContext.GetAgent();
            return Ok(await _markupBonusDisplayService.GetBonusesSummary(filter, agent));
        }
        

        private readonly IAgentContextService _agentContext;
        private readonly IAgentMarkupPolicyManager _policyManager;
        private readonly IMarkupPolicyTemplateService _policyTemplateService;
        private readonly IMarkupBonusDisplayService _markupBonusDisplayService;
    }
}