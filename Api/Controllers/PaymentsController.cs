using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
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
        public PaymentsController(IPaymentService paymentService, ICustomerContext customerContext)
        {
            _paymentService = paymentService;
            _customerContext = customerContext;
        }

        /// <summary>
        ///     Returns available currencies
        /// </summary>
        /// <returns>List of currencies.</returns>
        [HttpGet("currencies")]
        [ProducesResponseType(typeof(IReadOnlyCollection<Currencies>), (int) HttpStatusCode.OK)]
        public IActionResult GetCurrencies()
        {
            return Ok(_paymentService.GetCurrencies());
        }

        /// <summary>
        ///     Returns methods available for customer payments
        /// </summary>
        /// <returns>List of payment methods.</returns>
        [HttpGet("methods")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PaymentMethods>),(int) HttpStatusCode.OK)]
        public IActionResult GetPaymentMethods()
        {
            return Ok(_paymentService.GetAvailableCustomerPaymentMethods());
        }


        /// <summary>
        ///     Appends money to specified account.
        /// </summary>
        /// <param name="accountId">Id of account to add money.</param>
        /// <param name="paymentData">Payment details.</param>
        /// <returns></returns>
        [HttpPost("{accountId}/replenish")]
        [ProducesResponseType(typeof(IReadOnlyCollection<PaymentMethods>),(int) HttpStatusCode.NoContent)]
        public async Task<IActionResult> ReplenishAccount(int accountId, [FromBody] PaymentData paymentData)
        {
            var (isSuccess, _, error) = await _paymentService.ReplenishAccount(accountId, paymentData);
            return isSuccess 
                ? (IActionResult) NoContent()
                : (IActionResult) BadRequest(ProblemDetailsBuilder.Build(error));
        }

        /// <summary>
        ///     Pays by payfort token
        /// </summary>
        /// <param name="request">Payment request</param>
        [HttpPost()]
        [ProducesResponseType(typeof(PaymentResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Pay(PaymentRequest request)
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return OkOrBadRequest(await _paymentService.Pay(request, LanguageCode, GetClientIp(), customerInfo));
        }

        /// <summary>
        ///     Processes payment callback
        /// </summary>
        [HttpPost("callback")]
        [ProducesResponseType(typeof(PaymentResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PaymentCallback([FromBody]JObject value)
        {
            return OkOrBadRequest(await _paymentService.ProcessPaymentResponse(value));
        }

        /// <summary>
        ///     Returns true if payment with company account is available
        /// </summary>
        /// <returns>Payment with company account is available</returns>
        [HttpGet("accounts/available")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CanPayWithAccount()
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(await _paymentService.CanPayWithAccount(customerInfo));
        }


        private readonly IPaymentService _paymentService;
        private readonly ICustomerContext _customerContext;
    }
}
