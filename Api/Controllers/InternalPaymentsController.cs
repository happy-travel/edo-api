using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/internal/payments")]
    public class InternalPaymentsController : BaseController
    {
        public InternalPaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }


        /// <summary>
        ///     Complete payments after check-in date of booking
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CompletePayments(ProcessPaymentsInfo model)
        {
            return OkOrBadRequest(await _paymentService.CompletePayments(model.Date));
        }

        private readonly IPaymentService _paymentService;
    }
}