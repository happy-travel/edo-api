using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/internal/bookings")]
    public class InternalBookingsController : BaseController
    {
        public InternalBookingsController(IBookingsProcessingService bookingsProcessingService, RequestMetadataProvider requestMetadataProvider)
        {
            _bookingsProcessingService = bookingsProcessingService;
            _requestMetadataProvider = requestMetadataProvider;
        }


        /// <summary>
        ///     Gets bookings for cancellation by deadline date
        /// </summary>
        /// <param name="deadlineDate">Deadline date</param>
        /// <returns>List of booking ids for cancellation</returns>
        [HttpGet("cancel/{deadlineDate}")]
        [ProducesResponseType(typeof(List<int>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingsForCancellation(DateTime deadlineDate)
            => OkOrBadRequest(await _bookingsProcessingService.GetForCancellation(deadlineDate));


        /// <summary>
        ///     Cancels bookings
        /// </summary>
        /// <param name="bookingIds">List of booking ids for cancellation</param>
        /// <returns>Result message</returns>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(ProcessResult), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CancelBookings(List<int> bookingIds) => OkOrBadRequest(await _bookingsProcessingService.Cancel(bookingIds, _requestMetadataProvider.Get()));


        private readonly IBookingsProcessingService _bookingsProcessingService;
        private readonly RequestMetadataProvider _requestMetadataProvider;
    }
}