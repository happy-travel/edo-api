using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using IdentityModel.Client;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AgentsController : BaseController
    {
        public AgentsController(IAgentRegistrationService agentRegistrationService, IAgentContextService agentContextService,
            IAgentContextInternal agentContextInternal,
            IAgentInvitationService agentInvitationService,
            ITokenInfoAccessor tokenInfoAccessor,
            IAgentSettingsManager agentSettingsManager,
            IAgentPermissionManagementService permissionManagementService,
            IHttpClientFactory httpClientFactory,
            IAgentService agentService,
            IAgentStatusManagementService agentStatusManagementService)
        {
            _agentRegistrationService = agentRegistrationService;
            _agentContextService = agentContextService;
            _agentContextInternal = agentContextInternal;
            _agentInvitationService = agentInvitationService;
            _tokenInfoAccessor = tokenInfoAccessor;
            _agentSettingsManager = agentSettingsManager;
            _permissionManagementService = permissionManagementService;
            _httpClientFactory = httpClientFactory;
            _agentService = agentService;
            _agentStatusManagementService = agentStatusManagementService;
        }


        /// <summary>
        ///     Registers master agent with related counterparty
        /// </summary>
        /// <param name="request">Master agent registration request.</param>
        /// <returns></returns>
        [HttpPost("agent/register-master")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterAgentWithCounterparty([FromBody] RegisterAgentWithCounterpartyRequest request)
        {
            var externalIdentity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(externalIdentity))
                return BadRequest(ProblemDetailsBuilder.Build("User sub claim is required"));

            var email = await GetUserEmail();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(ProblemDetailsBuilder.Build("E-mail claim is required"));

            var registerResult = await _agentRegistrationService.RegisterWithCounterparty(request.Agent, request.Counterparty,
                externalIdentity, email);

            if (registerResult.IsFailure)
                return BadRequest(ProblemDetailsBuilder.Build(registerResult.Error));

            return NoContent();
        }


        /// <summary>
        ///     Registers regular agent.
        /// </summary>
        /// <param name="request">Regular agent registration request.</param>
        /// <returns></returns>
        [HttpPost("agent/register")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterInvitedAgent([FromBody] RegisterInvitedAgentRequest request)
        {
            var identity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(identity))
                return BadRequest(ProblemDetailsBuilder.Build("User sub claim is required"));

            var email = await GetUserEmail();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(ProblemDetailsBuilder.Build("E-mail claim is required"));

            var (_, isFailure, error) = await _agentRegistrationService
                .RegisterInvited(request.RegistrationInfo, request.InvitationCode, identity, email);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Invite regular agent by e-mail.
        /// </summary>
        /// <param name="request">Regular agent registration request.</param>
        /// <returns></returns>
        [HttpPost("agent/invitations/send")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AgentInvitation)]
        public async Task<IActionResult> InviteAgent([FromBody] SendAgentInvitationRequest request)
        {
            var (_, isFailure, error) = await _agentInvitationService.Send(request, await _agentContextService.GetAgent());
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///    Resend agent invitation
        /// </summary>
        /// <param name="invitationCode">Invitation code</param>>
        [HttpPost("agent/invitations/{invitationCode}/resend")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AgentInvitation)]
        public async Task<IActionResult> Resend(string invitationCode)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, error) = await _agentInvitationService.Resend(invitationCode, agent);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }

        /// <summary>
        ///     Creates invitation for regular agent.
        /// </summary>
        /// <param name="request">Regular agent registration request.</param>
        /// <returns>Invitation code.</returns>
        [HttpPost("agent/invitations/generate")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgentInvitation)]
        public async Task<IActionResult> CreateInvitation([FromBody] SendAgentInvitationRequest request)
        {
            var (_, isFailure, code, error) = await _agentInvitationService.Create(request, await _agentContextService.GetAgent());
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(code);
        }


        /// <summary>
        ///     Gets invitation data.
        /// </summary>
        /// <param name="code">Invitation code.</param>
        /// <returns>Invitation data, including pre-filled registration information.</returns>
        [HttpGet("agent/invitations/{code}")]
        [ProducesResponseType(typeof(AgentInvitationInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetInvitationData(string code)
        {
            var (_, isFailure, invitationInfo, error) = await _agentInvitationService
                .GetPendingInvitation(code);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(invitationInfo);
        }
        
        
        /// <summary>
        ///     Disable invitation.
        /// </summary>
        /// <param name="code">Invitation code.</param>
        [HttpPost("agent/invitations/{code}/disable")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DisableInvitation(string code)
        {
            var (_, isFailure, error) = await _agentInvitationService.Disable(code);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok();
        }


        /// <summary>
        ///    Gets agency not accepted invitations list
        /// </summary>
        [HttpGet("agency/invitations")]
        [ProducesResponseType(typeof(List<AgentInvitationResponse>), (int) HttpStatusCode.OK)]
        [InAgencyPermissions(InAgencyPermissions.ObserveAgencyInvitations)]
        public async Task<ActionResult<List<AgentInvitationResponse>>> GetAgencyNotAcceptedInvitations()
        {
            var agent = await _agentContextService.GetAgent();
            return await _agentInvitationService.GetAgencyNotAcceptedInvitations(agent.AgencyId);
        }


        /// <summary>
        ///    Gets agent not accepted invitations list
        /// </summary>
        [HttpGet("agent/invitations")]
        [ProducesResponseType(typeof(List<AgentInvitationResponse>), (int) HttpStatusCode.OK)]
        public async Task<ActionResult<List<AgentInvitationResponse>>> GetAgentNotAcceptedInvitations()
        {
            var agent = await _agentContextService.GetAgent();
            return await _agentInvitationService.GetAgentNotAcceptedInvitations(agent.AgentId);
        }
        
        
        /// <summary>
        ///    Gets agency accepted invitations list
        /// </summary>
        [HttpGet("agency/invitations/accepted")]
        [ProducesResponseType(typeof(List<AgentInvitationResponse>), (int) HttpStatusCode.OK)]
        [InAgencyPermissions(InAgencyPermissions.ObserveAgencyInvitations)]
        public async Task<ActionResult<List<AgentInvitationResponse>>> GetAgencyAcceptedInvitations()
        {
            var agent = await _agentContextService.GetAgent();
            return await _agentInvitationService.GetAgencyAcceptedInvitations(agent.AgencyId);
        }


        /// <summary>
        ///    Gets agent accepted invitations list
        /// </summary>
        [HttpGet("agent/invitations/accepted")]
        [ProducesResponseType(typeof(List<AgentInvitationResponse>), (int) HttpStatusCode.OK)]
        public async Task<ActionResult<List<AgentInvitationResponse>>> GetAgentAcceptedInvitations()
        {
            var agent = await _agentContextService.GetAgent();
            return await _agentInvitationService.GetAgentAcceptedInvitations(agent.AgentId);
        }


        /// <summary>
        ///     Gets current agent information
        /// </summary>
        /// <returns>Current agent information.</returns>
        [HttpGet("agent")]
        [ProducesResponseType(typeof(AgentDescription), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCurrentAgent()
        {
            var (_, isFailure, agent, error) = await _agentContextInternal.GetAgentInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            var agentAgencies = await _agentService.GetAgentRelations(agent);

            return Ok(new AgentDescription(agent.AgentId,
                agent.Email,
                agent.LastName,
                agent.FirstName,
                agent.Title,
                agent.Position,
                agentAgencies));
        }


        /// <summary>
        ///     Updates current agent properties.
        /// </summary>
        [HttpPut("agent/properties")]
        [ProducesResponseType(typeof(AgentEditableInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async Task<IActionResult> UpdateCurrentAgent([FromBody] AgentEditableInfo newInfo)
        {
            var agentRegistrationInfo = await _agentService.UpdateCurrentAgent(newInfo, await _agentContextService.GetAgent());
            return Ok(agentRegistrationInfo);
        }


        /// <summary>
        ///     Gets all agents of an agency
        /// </summary>
        [HttpGet("agency/agents")]
        [ProducesResponseType(typeof(List<SlimAgentInfo>), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.ObserveAgents)]
        [EnableQuery]
        public async Task<ActionResult<IQueryable<SlimAgentInfo>>> GetAgents()
        {
            return Ok(_agentService.GetAgents(await _agentContextService.GetAgent()));
        }


        /// <summary>
        ///     Gets agent of a specified agency
        /// </summary>
        [HttpGet("agency/agents/{agentId}")]
        [ProducesResponseType(typeof(AgentInfoInAgency), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.PermissionManagement)]
        public async Task<IActionResult> GetAgentInfo(int agentId)
        {
            var (_, isFailure, agent, error) = await _agentService.GetAgent(agentId, await _agentContextService.GetAgent());
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(agent);
        }


        /// <summary>
        ///     Updates permissions agent permissions
        /// </summary>
        [HttpPut("agency/agents/{agentId}/permissions")]
        [ProducesResponseType(typeof(List<InAgencyPermissions>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.PermissionManagement)]
        public async Task<IActionResult> UpdatePermissionsInAgency(int agentId,
            [FromBody] List<InAgencyPermissions> newPermissions)
        {
            var (_, isFailure, permissions, error) = await _permissionManagementService
                .SetInAgencyPermissions(agentId, newPermissions, await _agentContextService.GetAgent());

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(permissions);
        }


        /// <summary>
        ///     Sets user frontend application settings.
        /// </summary>
        /// <param name="settings">Settings in dynamic JSON-format</param>
        /// <returns></returns>
        [RequestSizeLimit(256 * 1024)]
        [HttpPut("agent/settings/application")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AgentRequired]
        public async Task<IActionResult> SetApplicationSettings([FromBody] JToken settings)
        {
            var agentInfo = await _agentContextService.GetAgent();
            await _agentSettingsManager.SetAppSettings(agentInfo, settings);
            return NoContent();
        }


        /// <summary>
        ///     Gets user frontend application settings.
        /// </summary>
        /// <returns>Settings in dynamic JSON-format</returns>
        [HttpGet("agent/settings/application")]
        [ProducesResponseType(typeof(JToken), (int) HttpStatusCode.OK)]
        [AgentRequired]
        public async Task<IActionResult> GetApplicationSettings()
        {
            var agentInfo = await _agentContextService.GetAgent();
            var settings = await _agentSettingsManager.GetAppSettings(agentInfo);
            return Ok(settings);
        }


        /// <summary>
        ///     Sets user preferences.
        /// </summary>
        /// <param name="settings">Settings in JSON-format</param>
        /// <returns></returns>
        [HttpPut("agent/settings/user")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AgentRequired]
        public async Task<IActionResult> SetUserSettings([FromBody] AgentUserSettings settings)
        {
            var agentInfo = await _agentContextService.GetAgent();
            await _agentSettingsManager.SetUserSettings(agentInfo, settings);
            return NoContent();
        }


        /// <summary>
        ///     Gets user preferences.
        /// </summary>
        /// <returns>Settings in JSON-format</returns>
        [HttpGet("agent/settings/user")]
        [ProducesResponseType(typeof(AgentUserSettings), (int) HttpStatusCode.OK)]
        [AgentRequired]
        public async Task<IActionResult> GetUserSettings()
        {
            var agentInfo = await _agentContextService.GetAgent();
            var settings = await _agentSettingsManager.GetUserSettings(agentInfo);
            return Ok(settings);
        }


        /// <summary>
        ///     Gets all possible permissions
        /// </summary>
        /// <returns> Array of all permission names </returns>
        [HttpGet("all-permissions-list")]
        [ProducesResponseType(typeof(IEnumerable<InAgencyPermissions>), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        public IActionResult GetAllPermissionsList() 
            => Ok(InAgencyPermissions.All.ToList().Where(p => p != InAgencyPermissions.All));


        private async Task<string> GetUserEmail()
        {
            // TODO: Move this logic to separate service
            using var identityClient = _httpClientFactory.CreateClient(HttpClientNames.Identity);

            var doc = await identityClient.GetDiscoveryDocumentAsync();
            var token = await _tokenInfoAccessor.GetAccessToken();

            return (await identityClient.GetUserInfoAsync(new UserInfoRequest {Token = token, Address = doc.UserInfoEndpoint}))
                .Claims
                .SingleOrDefault(c => c.Type == "email")
                ?.Value;
        }


        /// <summary>
        ///     Enables a given agent to operate using a given agency
        /// </summary>
        [HttpPost("agency/agents/{agentId}/enable")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgentStatusManagement)]
        public async Task<IActionResult> Enable(int agentId) 
            => OkOrBadRequest(await _agentStatusManagementService.Enable(agentId, await _agentContextService.GetAgent()));
        

        /// <summary>
        ///     Disables a given agent to operate using a given agency
        /// </summary>
        [HttpPost("agency/agents/{agentId}/disable")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgentStatusManagement)]
        public async Task<IActionResult> Disable(int agentId) 
            => OkOrBadRequest(await _agentStatusManagementService.Disable(agentId, await _agentContextService.GetAgent()));


        private readonly IAgentContextService _agentContextService;
        private readonly IAgentContextInternal _agentContextInternal;
        private readonly IAgentInvitationService _agentInvitationService;
        private readonly IAgentRegistrationService _agentRegistrationService;
        private readonly IAgentSettingsManager _agentSettingsManager;
        private readonly IAgentPermissionManagementService _permissionManagementService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAgentService _agentService;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
        private readonly IAgentStatusManagementService _agentStatusManagementService;
    }
}