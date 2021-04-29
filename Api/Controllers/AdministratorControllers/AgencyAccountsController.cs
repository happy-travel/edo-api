using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Management;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin")]
    [Produces("application/json")]
    public class AgencyAccountsController : BaseController
    {
        public AgencyAccountsController(IAdministratorContext administratorContext, IAgencyAccountService agencyAccountService)
        {
            _administratorContext = administratorContext;
            _agencyAccountService = agencyAccountService;
        }


        /// <summary>
        ///     Gets agency accounts list
        /// </summary>
        /// <param name="agencyId">Agency Id</param>
        [HttpGet("agencies/{agencyId}/accounts")]
        [ProducesResponseType(typeof(List<FullAgencyAccountInfo>), (int) HttpStatusCode.OK)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceObservation)]
        public async Task<IActionResult> GetAgencyAccounts([FromRoute] int agencyId) 
            => Ok(await _agencyAccountService.Get(agencyId));


        /// <summary>
        /// Activates specified agency account.
        /// </summary>
        /// <param name="agencyId">Agency Id.</param>
        /// <param name="agencyAccountId">Agency account Id.</param>
        /// <param name="activityStatusChangeRequest">Request data for activation.</param>
        [HttpPost("agencies/{agencyId}/accounts/{agencyAccountId}/activate")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> ActivateAgencyAccount([FromRoute] int agencyId, [FromRoute] int agencyAccountId,
            [FromBody] ActivityStatusChangeRequest activityStatusChangeRequest)
            => OkOrBadRequest(await _agencyAccountService.Activate(agencyId, agencyAccountId, activityStatusChangeRequest.Reason));


        /// <summary>
        /// Deactivates specified agency account.
        /// </summary>
        /// <param name="agencyId">Agency Id.</param>
        /// <param name="agencyAccountId">Agency account Id.</param>
        /// <param name="activityStatusChangeRequest">Request data for deactivation.</param>
        [HttpPost("agencies/{agencyId}/accounts/{agencyAccountId}/deactivate")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> DeactivateAgencyAccount([FromRoute] int agencyId, [FromRoute] int agencyAccountId,
            [FromBody] ActivityStatusChangeRequest activityStatusChangeRequest)
            => OkOrBadRequest(await _agencyAccountService.Deactivate(agencyId, agencyAccountId, activityStatusChangeRequest.Reason));


        /// <summary>
        ///     Manually Adds money to the agency account
        /// </summary>
        /// <param name="agencyAccountId">Id of the agency account</param>
        /// <param name="paymentData">Details about the payment</param>
        [HttpPost("agency-accounts/{agencyAccountId}/increase-manually")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.BalanceManualCorrection)]
        public async Task<IActionResult> IncreaseMoneyManuallyToAgencyAccount(int agencyAccountId, [FromBody] PaymentData paymentData)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _agencyAccountService.IncreaseManually(agencyAccountId, paymentData,
                administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Manually Subtracts money from the agency account
        /// </summary>
        /// <param name="agencyAccountId">Id of the agency account</param>
        /// <param name="paymentData">Details about the payment</param>
        [HttpPost("agency-accounts/{agencyAccountId}/decrease-manually")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.BalanceManualCorrection)]
        public async Task<IActionResult> DecreaseMoneyManuallyFromAgencyAccount(int agencyAccountId, [FromBody] PaymentData paymentData)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _agencyAccountService.DecreaseManually(agencyAccountId, paymentData,
                administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        private readonly IAgencyAccountService _agencyAccountService;
        private readonly IAdministratorContext _administratorContext;
    }
}