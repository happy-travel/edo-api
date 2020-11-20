using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Booking;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/payments")]
    [Produces("application/json")]
    public class PaymentsController : BaseController
    {
        public PaymentsController(IBookingPaymentService bookingPaymentService,
            IAdministratorContext administratorContext, ICounterpartyAccountService counterpartyAccountService, IAgencyAccountService agencyAccountService,
            IBookingCreditCardPaymentConfirmationService bookingCreditCardConfirmationService)
        {
            _bookingPaymentService = bookingPaymentService;
            _administratorContext = administratorContext;
            _counterpartyAccountService = counterpartyAccountService;
            _agencyAccountService = agencyAccountService;
            _bookingCreditCardConfirmationService = bookingCreditCardConfirmationService;
        }


        /// <summary>
        ///     Completes payment manually
        /// </summary>
        /// <param name="bookingId">Booking id for completion</param>
        [HttpPost("offline/{bookingId}")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.OfflinePayment)]
        public async Task<IActionResult> CompleteOffline(int bookingId)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (_, isFailure, error) = await _bookingPaymentService.CompleteOffline(bookingId, administrator);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Gets balance for a counterparty account
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty</param>
        /// <param name="currency">Currency</param>
        [HttpGet("counterparties/{counterpartyId}/balance/{currency}")]
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


        /// <summary>
        ///     Confirm credit card payment
        /// </summary>
        [HttpGet("credit-card/{bookingId}/confirm")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.OfflinePayment)]
        public async Task<IActionResult> ConfirmCreditCartPayment(int bookingId)
        {
            var (_, _, administrator, _) = await _administratorContext.GetCurrent();
            var (isSuccess, _, booking, error) = await _bookingCreditCardConfirmationService.Confirm(bookingId, administrator);

            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(error);
        }

        private readonly IAgencyAccountService _agencyAccountService;
        private readonly IAdministratorContext _administratorContext;
        private readonly IBookingPaymentService _bookingPaymentService;
        private readonly IBookingCreditCardPaymentConfirmationService _bookingCreditCardConfirmationService;
        private readonly ICounterpartyAccountService _counterpartyAccountService;
    }
}