using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Invitations;
using HappyTravel.Edo.Api.Services;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Invitations;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AgenciesController : BaseController
    {
        public AgenciesController(IChildAgencyService childAgencyService,
            IAgentContextService agentContextService,
            IAgentInvitationCreateService agentInvitationCreateService,
            IAgencyManagementService agencyManagementService,
            IAgentInvitationAcceptService agentInvitationAcceptService,
            ITokenInfoAccessor tokenInfoAccessor,
            IAgencyService agencyService)
        {
            _childAgencyService = childAgencyService;
            _agentContextService = agentContextService;
            _agentInvitationCreateService = agentInvitationCreateService;
            _agencyManagementService = agencyManagementService;
            _agentInvitationAcceptService = agentInvitationAcceptService;
            _tokenInfoAccessor = tokenInfoAccessor;
            _agencyService = agencyService;
        }


        /// <summary>
        ///     Gets current agent's agency.
        /// </summary>
        /// <returns></returns>
        [HttpGet("agency")]
        [ProducesResponseType(typeof(SlimAgencyInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async Task<IActionResult> GetSelfAgency()
        {
            var (_, isFailure, agency, error) = await _agencyService.Get(await _agentContextService.GetAgent(), LanguageCode);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(agency);
        }


        [HttpPut("agency")]
        [ProducesResponseType(typeof(SlimAgencyInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.PermissionManagement)]
        public async Task<IActionResult> EditSelfAgency([FromBody] EditAgencyRequest request)
        {
            var (_, isFailure, agency, error) = await _agencyService.Edit(await _agentContextService.GetAgent(), request, LanguageCode);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(agency);
        }


        /// <summary>
        ///     Gets agency.
        /// </summary>
        /// <param name="agencyId">Agency Id.</param>
        /// <returns></returns>
        [HttpGet("agency/child-agencies/{agencyId}")]
        [ProducesResponseType(typeof(ChildAgencyInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.ObserveChildAgencies)]
        public async Task<IActionResult> GetChildAgency(int agencyId)
        {
            var (_, isFailure, agency, error) = await _childAgencyService.Get(agencyId, await _agentContextService.GetAgent());

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(agency);
        }


        /// <summary>
        ///     Gets child agencies.
        /// </summary>
        /// <returns></returns>
        [HttpGet("agency/child-agencies")]
        [ProducesResponseType(typeof(List<SlimChildAgencyInfo>), (int)HttpStatusCode.OK)]
        [InAgencyPermissions(InAgencyPermissions.ObserveChildAgencies)]
        public async Task<IActionResult> GetChildAgencies()
            => Ok(await _childAgencyService.Get(await _agentContextService.GetAgent()));


        /// <summary>
        ///     Sends an email inviting to create a child agency.
        /// </summary>
        /// <param name="request">Request for child agency invitation</param>
        /// <returns></returns>
        [HttpPost("agency/invitations/send")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.InviteChildAgencies)]
        public async Task<IActionResult> InviteChildAgency([FromBody] CreateChildAgencyInvitationRequest request)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, _, error) = await _agentInvitationCreateService.Send(request.ToUserInvitationData(),
                UserInvitationTypes.ChildAgency, agent.AgentId, agent.AgencyId);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Resends an email inviting to create a child agency.
        /// </summary>
        /// <param name="invitationCodeHash">Invitation code hash</param>>
        /// <returns></returns>
        [HttpPost("agency/invitations/{invitationCodeHash}/resend")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.InviteChildAgencies)]
        public async Task<IActionResult> ResendInvitationChildAgency([FromRoute] string invitationCodeHash)
        {
            var (_, isFailure, _, error) = await _agentInvitationCreateService.Resend(invitationCodeHash);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Invites to create child agency.
        /// </summary>
        /// <param name="request">Request for child agency invitation</param>
        /// <returns>Invitation code</returns>
        [HttpPost("agency/invitations/generate")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.InviteChildAgencies)]
        public async Task<IActionResult> GenerateChildAgencyInvite([FromBody] CreateChildAgencyInvitationRequest request)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, code, error) = await _agentInvitationCreateService.Create(request.ToUserInvitationData(),
                UserInvitationTypes.ChildAgency, agent.AgentId, agent.AgencyId);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(code);
        }


        /// <summary>
        ///     Recreates the invitation to create child agency.
        /// </summary>
        /// <param name="invitationCodeHash">Invitation code hash</param>>
        /// <returns>Invitation code</returns>
        [HttpPost("agency/invitations/{invitationCodeHash}/regenerate")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.InviteChildAgencies)]
        public async Task<IActionResult> RegenerateChildAgencyInvite([FromRoute] string invitationCodeHash)
        {
            var (_, isFailure, code, error) = await _agentInvitationCreateService.Recreate(invitationCodeHash);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(code);
        }


        /// <summary>
        ///     Accepts invitation to create child agency.
        /// </summary>
        /// <returns></returns>
        [HttpPost("agency/register")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AcceptChildAgencyInvite([FromBody] RegisterInvitedAgencyRequest request)
        {
            var identity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(identity))
                return BadRequest(ProblemDetailsBuilder.Build("User sub claim is required"));

            var (_, isFailure, error) = await _agentInvitationAcceptService.Accept(
                request.InvitationCode,
                request.ToUserInvitationData(),
                identity);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///  Deactivates specified agency.
        /// </summary>
        /// <param name="agencyId">Id of the agency.</param>
        /// <returns></returns>
        [HttpPost("agency/child-agencies/{agencyId}/deactivate")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeactivateAgency(int agencyId)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, error) = await _agencyManagementService.DeactivateChildAgency(agencyId, agent);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///  Activates specified agency.
        /// </summary>
        /// <param name="agencyId">Id of the agency.</param>
        /// <returns></returns>
        [HttpPost("agency/child-agencies/{agencyId}/activate")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ActivateAgency(int agencyId)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, error) = await _agencyManagementService.ActivateChildAgency(agencyId, agent);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        private readonly IChildAgencyService _childAgencyService;
        private readonly IAgentContextService _agentContextService;
        private readonly IAgentInvitationCreateService _agentInvitationCreateService;
        private readonly IAgentInvitationAcceptService _agentInvitationAcceptService;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
        private readonly IAgencyService _agencyService;
        private readonly IAgencyManagementService _agencyManagementService;
    }
}
