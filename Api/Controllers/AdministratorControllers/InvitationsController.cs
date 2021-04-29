using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices.Invitations;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Invitations;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Management;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/invitations")]
    [Produces("application/json")]
    public class InvitationsController : BaseController
    {
        public InvitationsController(IInvitationRecordService invitationRecordService,
            IAdminInvitationAcceptService adminInvitationAcceptService,
            IAdminInvitationCreateService adminInvitationCreateService,
            ITokenInfoAccessor tokenInfoAccessor,
            IAdministratorContext administratorContext)
        {
            _invitationRecordService = invitationRecordService;
            _adminInvitationAcceptService = adminInvitationAcceptService;
            _adminInvitationCreateService = adminInvitationCreateService;
            _tokenInfoAccessor = tokenInfoAccessor;
            _administratorContext = administratorContext;
        }


        /// <summary>
        ///     Send invitation to administrator.
        /// </summary>
        /// <param name="invitationInfo">Administrator invitation info.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [AdministratorPermissions(AdministratorPermissions.AdministratorInvitation)]
        public async Task<IActionResult> InviteAdministrator([FromBody] UserDescriptionInfo invitationInfo)
        {
            var (_, _, admin, _) = await _administratorContext.GetCurrent();

            var (_, isFailure, error) = await _adminInvitationCreateService.Send(invitationInfo, admin.Id);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Register current user as administrator by invitation code.
        /// </summary>
        /// <param name="invitationCode">Invitation code.</param>
        /// <returns></returns>
        [HttpPost("accept")]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        public async Task<IActionResult> RegisterAdministrator([FromBody] string invitationCode)
        {
            var identity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(identity))
                return BadRequest(ProblemDetailsBuilder.Build("Could not get user's identity"));

            var (_, isFailure, error) = await _adminInvitationAcceptService.Accept(invitationCode, default, identity);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        /// <summary>
        ///     Disable invitation.
        /// </summary>
        /// <param name="codeHash">Invitation code hash.</param>
        [HttpPost("{codeHash}/disable")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DisableInvitation(string codeHash)
        {
            var (_, isFailure, error) = await _invitationRecordService.Revoke(codeHash);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok();
        }
        
        
        private readonly IInvitationRecordService _invitationRecordService;
        private readonly IAdminInvitationAcceptService _adminInvitationAcceptService;
        private readonly IAdminInvitationCreateService _adminInvitationCreateService;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
        private readonly IAdministratorContext _administratorContext;
    }
}