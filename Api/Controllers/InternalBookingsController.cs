using System;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/internal/bookings")]
    public class InternalBookingsController : BaseController
    {
        public InternalBookingsController(IAccommodationService accommodationService)
        {
            _accommodationService = accommodationService;
        }


        /// <summary>
        ///     Gets bookings for cancellation by deadline date
        /// </summary>
        [HttpGet("cancel/{deadlineDate}")]
        [ProducesResponseType(typeof(ListOfBookingIds), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingsForCancellation(DateTime deadlineDate)
        {
            return OkOrBadRequest(await _accommodationService.GetBookingsForCancellation(deadlineDate));
        }


        /// <summary>
        ///     Cancels bookings
        /// </summary>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CancelBookings(ListOfBookingIds model)
        {
            return OkOrBadRequest(await _accommodationService.CancelBookings(model));
        }

        private readonly IAccommodationService _accommodationService;
    }
}