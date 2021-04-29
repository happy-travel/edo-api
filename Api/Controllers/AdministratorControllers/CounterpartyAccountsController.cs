using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin")]
    [Produces("application/json")]
    public class CounterpartyAccountsController : BaseController
    {
        public CounterpartyAccountsController(IAdministratorContext administratorContext, ICounterpartyAccountService counterpartyAccountService)
        {
            _administratorContext = administratorContext;
            _counterpartyAccountService = counterpartyAccountService;
        }


        /// <summary>
        ///     Gets balance for a counterparty account
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty</param>
        /// <param name="currency">Currency</param>
        [HttpGet("counterparties/{counterpartyId}/counterparty-accounts/{currency}/balance")]
        [ProducesResponseType(typeof(CounterpartyBalanceInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceObservation)]
        public async Task<IActionResult> GetCounterpartyBalance(int counterpartyId, Currencies currency)
            => OkOrBadRequest(await _counterpartyAccountService.GetBalance(counterpartyId, currency));


        /// <summary>
        ///     Appends money to a counterparty account
        /// </summary>
        /// <param name="counterpartyAccountId">Id of the counterparty account</param>
        /// <param name="paymentData">Details about the payment</param>
        [HttpPost("counterparty-accounts/{counterpartyAccountId}/replenish")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> ReplenishCounterpartyAccount(int counterpartyAccountId, [FromBody] PaymentData paymentData)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _counterpartyAccountService.AddMoney(counterpartyAccountId, paymentData,
                administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Subtracts money from a counterparty account due to payment cancellation
        /// </summary>
        /// <param name="counterpartyAccountId">Id of the counterparty account</param>
        /// <param name="cancellationData">Details about the payment cancellation</param>
        [HttpPost("counterparty-accounts/{counterpartyAccountId}/subtract")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> SubtractCounterpartyAccount(int counterpartyAccountId, [FromBody] PaymentCancellationData cancellationData)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _counterpartyAccountService.SubtractMoney(counterpartyAccountId,
                cancellationData, administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Transfers money from a counterparty account to the default agency account
        /// </summary>
        /// <param name="counterpartyAccountId">Id of the counterparty account</param>
        /// <param name="amount">Amount of money to transfer</param>
        [HttpPost("counterparty-accounts/{counterpartyAccountId}/transfer")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyToAgencyTransfer)]
        public async Task<IActionResult> TransferToDefaultAgency(int counterpartyAccountId, [FromBody] MoneyAmount amount)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _counterpartyAccountService.TransferToDefaultAgency(counterpartyAccountId, amount,
                administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Manually Adds money to the counterparty account
        /// </summary>
        /// <param name="counterpartyAccountId">Id of the counterparty account</param>
        /// <param name="paymentData">Details about the payment</param>
        [HttpPost("counterparty-accounts/{counterpartyAccountId}/increase-manually")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.BalanceManualCorrection)]
        public async Task<IActionResult> IncreaseMoneyManuallyToCounterpartyAccount(int counterpartyAccountId, [FromBody] PaymentData paymentData)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _counterpartyAccountService.IncreaseManually(counterpartyAccountId, paymentData,
                administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Manually subtracts money from the counterparty account
        /// </summary>
        /// <param name="counterpartyAccountId">Id of the counterparty account</param>
        /// <param name="paymentData">Details about the payment</param>
        [HttpPost("counterparty-accounts/{counterpartyAccountId}/decrease-manually")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.BalanceManualCorrection)]
        public async Task<IActionResult> DecreaseMoneyManuallyFromCounterpartyAccount(int counterpartyAccountId, [FromBody] PaymentData paymentData)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, error) = await _counterpartyAccountService.DecreaseManually(counterpartyAccountId, paymentData,
                administrator.ToUserInfo());

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        /// Gets counterparty accounts list
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id</param>
        [HttpGet("counterparties/{counterpartyId}/accounts")]
        [ProducesResponseType(typeof(List<CounterpartyAccountInfo>), (int) HttpStatusCode.OK)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> GetAccounts(int counterpartyId) 
            => Ok(await _counterpartyAccountService.Get(counterpartyId));


        /// <summary>
        /// Activates specified counterparty account.
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id.</param>
        /// <param name="counterpartyAccountId">Counterparty account Id.</param>
        /// <param name="activityStatusChangeRequest">Request data for activation.</param>
        [HttpPost("counterparties/{counterpartyId}/accounts/{counterpartyAccountId}/activate")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> ActivateCounterpartyAccount([FromRoute] int counterpartyId, [FromRoute] int counterpartyAccountId,
            [FromBody] ActivityStatusChangeRequest activityStatusChangeRequest)
            => OkOrBadRequest(await _counterpartyAccountService.Activate(counterpartyId, counterpartyAccountId, activityStatusChangeRequest.Reason));


        /// <summary>
        /// Deactivates specified counterparty account.
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id.</param>
        /// <param name="counterpartyAccountId">Counterparty account Id.</param>
        /// <param name="activityStatusChangeRequest">Request data for deactivation.</param>
        [HttpPost("counterparties/{counterpartyId}/accounts/{counterpartyAccountId}/deactivate")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> DeactivateCounterpartyAccount([FromRoute] int counterpartyId, [FromRoute] int counterpartyAccountId,
            [FromBody] ActivityStatusChangeRequest activityStatusChangeRequest)
            => OkOrBadRequest(await _counterpartyAccountService.Deactivate(counterpartyId, counterpartyAccountId, activityStatusChangeRequest.Reason));


        private readonly IAdministratorContext _administratorContext;
        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}