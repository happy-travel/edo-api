using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/external/payments")]
    public class ExternalPaymentsController : BaseController
    {
        public ExternalPaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }


        /// <summary>
        ///     Processes payment callback
        /// </summary>
        [AllowAnonymous]
        [HttpPost("callback")]
        [ProducesResponseType(typeof(PaymentResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> PaymentCallback([FromForm]IFormCollection form)
        {
            if (form is null)
                return BadRequest("Payment data is required");
            var dictionary = form.ToDictionary(k => k.Key, k => WebUtility.UrlDecode(k.Value.ToString()));
            var value = JObject.FromObject(dictionary);
            return OkOrBadRequest(await _paymentService.ProcessPaymentResponse(value));
        }

        private readonly IPaymentService _paymentService;
    }
}