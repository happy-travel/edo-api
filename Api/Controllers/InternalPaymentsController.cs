using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.ServiceAccountFilters;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Management;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/internal/payments")]
    public class InternalPaymentsController : BaseController
    {
        public InternalPaymentsController(IBookingPaymentService paymentService, IServiceAccountContext serviceAccountContext)
        {
            _paymentService = paymentService;
            _serviceAccountContext = serviceAccountContext;
        }


        /// <summary>
        ///     Gets bookings for payment completion by deadline date
        /// </summary>
        /// <param name="deadlineDate">Deadline date</param>
        /// <returns>List of booking ids for capture</returns>
        [HttpGet("capture/{deadlineDate}")]
        [ProducesResponseType(typeof(List<int>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ServiceAccountRequired]
        public async Task<IActionResult> GetBookingsForCapture(DateTime deadlineDate)
            => OkOrBadRequest(await _paymentService.GetBookingsForCapture(deadlineDate));


        /// <summary>
        ///     Captures payments for bookings
        /// </summary>
        /// <param name="bookingIds">List of booking ids for capture</param>
        /// <returns>Result message</returns>
        [HttpPost("capture")]
        [ProducesResponseType(typeof(ProcessResult), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ServiceAccountRequired]
        public async Task<IActionResult> Capture(List<int> bookingIds)
        {
            var (_, _, serviceAccount, _) = await _serviceAccountContext.GetCurrent();
            return OkOrBadRequest(await _paymentService.CaptureMoneyForBookings(bookingIds, serviceAccount));
        }


        /// <summary>
        ///     Sends need payment notifications for bookings
        /// </summary>
        /// <param name="bookingIds">List of booking ids for notify</param>
        /// <returns>Result message</returns>
        [HttpPost("notify/need-payment")]
        [ProducesResponseType(typeof(ProcessResult), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [ServiceAccountRequired]
        public async Task<IActionResult> NotifyPaymentsNeeded(List<int> bookingIds) => OkOrBadRequest(await _paymentService.NotifyPaymentsNeeded(bookingIds));


        private readonly IBookingPaymentService _paymentService;
        private readonly IServiceAccountContext _serviceAccountContext;
    }
}