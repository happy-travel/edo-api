using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Api.Services.Payments.CreditCards;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/payments")]
    [Produces("application/json")]
    public class PaymentsController : BaseController
    {
        public PaymentsController(IAccountPaymentService accountPaymentService, ICreditCardPaymentService creditCardPaymentService,
            IPaymentService paymentService, ICustomerContext customerContext)
        {
            _accountPaymentService = accountPaymentService;
            _creditCardPaymentService = creditCardPaymentService;
            _paymentService = paymentService;
            _customerContext = customerContext;
        }


        /// <summary>
        ///     Returns available currencies
        /// </summary>
        /// <returns>List of currencies.</returns>
        [HttpGet("currencies")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Currencies>), (int) HttpStatusCode.OK)]
        public IActionResult GetCurrencies() => Ok(_paymentService.GetCurrencies());


        /// <summary>
        ///     Returns methods available for customer's payments
        /// </summary>
        /// <returns>List of payment methods.</returns>
        [HttpGet("methods")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PaymentMethods>), (int) HttpStatusCode.OK)]
        public IActionResult GetPaymentMethods() => Ok(_paymentService.GetAvailableCustomerPaymentMethods());


        /// <summary>
        ///     Appends money to specified account.
        /// </summary>
        /// <param name="accountId">Id of account to add money.</param>
        /// <param name="paymentData">Payment details.</param>
        /// <returns></returns>
        [HttpPost("{accountId}/replenish")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PaymentMethods>), (int) HttpStatusCode.NoContent)]
        public async Task<IActionResult> ReplenishAccount(int accountId, [FromBody] PaymentData paymentData)
        {
            var (isSuccess, _, error) = await _accountPaymentService.ReplenishAccount(accountId, paymentData);
            return isSuccess
                ? NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Pays by payfort token
        /// </summary>
        /// <param name="request">Payment request</param>
        [Obsolete]
        [HttpPost]
        [ProducesResponseType(typeof(PaymentResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public Task<IActionResult> Pay(PaymentRequest request) => PayWithCreditCard(request);


        /// <summary>
        ///     Pays by payfort token
        /// </summary>
        /// <param name="request">Payment request</param>
        [HttpPost("card")]
        [ProducesResponseType(typeof(PaymentResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PayWithCreditCard(PaymentRequest request)
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return OkOrBadRequest(await _creditCardPaymentService.AuthorizeMoney(request, LanguageCode, GetClientIp(), customerInfo));
        }


        /// <summary>
        ///     Pays from account
        /// </summary>
        /// <param name="request">Payment request</param>
        [HttpPost("account")]
        [ProducesResponseType(typeof(PaymentResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PayWithAccount(AccountPaymentRequest request)
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return OkOrBadRequest(await _accountPaymentService.AuthorizeMoney(request, customerInfo, GetClientIp()));
        }


        /// <summary>
        ///     Processes payment callback
        /// </summary>
        [HttpPost("callback")]
        [ProducesResponseType(typeof(PaymentResponse), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PaymentCallback([FromBody] JObject value)
            => OkOrBadRequest(await _creditCardPaymentService.ProcessPaymentResponse(value));


        /// <summary>
        ///     Returns true if payment with company account is available
        /// </summary>
        /// <returns>Payment with company account is available</returns>
        [HttpGet("accounts/available")]
        [ProducesResponseType(typeof(bool), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [Obsolete("Use accounts/balance instead")]
        public async Task<IActionResult> CanPayWithAccount()
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(await _accountPaymentService.CanPayWithAccount(customerInfo));
        }


        /// <summary>
        ///     Returns account balance for currency
        /// </summary>
        /// <returns>Account balance</returns>
        [HttpGet("accounts/balance/{currency}")]
        [ProducesResponseType(typeof(AccountBalanceInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public Task<IActionResult> GetAccountBalance(Currencies currency) => OkOrBadRequest(_accountPaymentService.GetAccountBalance(currency));


        /// <summary>
        ///     Completes payment manually
        /// </summary>
        /// <param name="bookingId">Booking id for completion</param>
        [HttpPost("offline/{bookingId}")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CompleteOffline(int bookingId)
        {
            var (_, isFailure, error) = await _paymentService.CompleteOffline(bookingId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Gets pending amount for booking
        /// </summary>
        /// <param name="bookingId">Booking id</param>
        [HttpPost("pending/{bookingId}")]
        [ProducesResponseType(typeof(Price), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public Task<IActionResult> GetPendingAmount(int bookingId) => OkOrBadRequest(_paymentService.GetPendingAmount(bookingId));


        private readonly ICustomerContext _customerContext;
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly ICreditCardPaymentService _creditCardPaymentService;
        private readonly IPaymentService _paymentService;
    }
}