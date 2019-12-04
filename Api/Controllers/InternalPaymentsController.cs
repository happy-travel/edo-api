using System;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Payments;
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
        ///     Gets bookings for payment completion by deadline date
        /// </summary>
        [HttpGet("complete/{deadlineDate}")]
        [ProducesResponseType(typeof(ListOfBookingIds), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingsForCompletion(DateTime deadlineDate)
        {
            return OkOrBadRequest(await _paymentService.GetBookingsForCompletion(deadlineDate));
        }


        /// <summary>
        ///     Completes payments for bookings
        /// </summary>
        [HttpPost("complete")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CompletePayments(ListOfBookingIds model)
        {
            return OkOrBadRequest(await _paymentService.Complete(model));
        }

        private readonly IPaymentService _paymentService;
    }
}