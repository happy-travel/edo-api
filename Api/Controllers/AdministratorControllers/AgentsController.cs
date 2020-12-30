using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Data.Agents;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin")]
    [Produces("application/json")]
    public class AgentsController : BaseController
    {
        public AgentsController(IAgentSystemSettingsManagementService systemSettingsManagementService,
            IAgentMovementService agentMovementService)
        {
            _systemSettingsManagementService = systemSettingsManagementService;
            _agentMovementService = agentMovementService;
        }
        
        
        /// <summary>
        /// Updates agent's availability search settings
        /// </summary>
        /// <param name="settings">Settings</param>
        /// <param name="agentId">Agent Id</param>
        /// <param name="agencyId">Agency Id</param>
        /// <returns></returns>
        [HttpPut("agencies/{agencyId}/agents/{agentId}/system-settings/availability-search")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.AgentManagement)]
        public async Task<IActionResult> SetSystemSettings([FromBody] AgentAccommodationBookingSettings settings, [FromRoute] int agentId, [FromRoute] int agencyId)
        {
            var (_, isFailure, error) = await _systemSettingsManagementService.SetAvailabilitySearchSettings(agentId, agencyId, settings);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok();
        }
        
        /// <summary>
        /// Gets agent's availability search settings
        /// </summary>
        /// <param name="agentId">Agent Id</param>
        /// <param name="agencyId">Agency Id</param>
        /// <returns></returns>
        [HttpGet("agencies/{agencyId}/agents/{agentId}/system-settings/availability-search")]
        [ProducesResponseType(typeof(AgentAccommodationBookingSettings), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.AgentManagement)]
        public async Task<IActionResult> GetSystemSettings([FromRoute] int agentId, [FromRoute] int agencyId)
        {
            var (_, isFailure, settings, error) = await _systemSettingsManagementService.GetAvailabilitySearchSettings(agentId, agencyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(settings);
        }
        
        
        /// <summary>
        /// Move agent from one agency to another
        /// <param name="agentId">Agent Id</param>
        /// <param name="agencyId">Source agency Id</param>
        /// <param name="targetAgencyId">Target agency Id</param>
        /// </summary>
        [HttpPost("agencies/{agencyId}/agents/{agentId}/change-agency")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.AgentManagement)]
        public async Task<IActionResult> MoveAgentToAgency([FromRoute] int agentId, [FromRoute] int agencyId, [FromBody] int targetAgencyId)
        {
            var (_, isFailure, error) = await _agentMovementService.Move(agentId, agencyId, targetAgencyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));
            
            return Ok();
        }
        
        
        private readonly IAgentSystemSettingsManagementService _systemSettingsManagementService;
        private readonly IAgentMovementService _agentMovementService;
    }
}