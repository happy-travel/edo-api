using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;
using SingleAccommodationAvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.SingleAccommodationAvailabilityRequest;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AccommodationsController : BaseController
    {
        public AccommodationsController(IAccommodationService service)
        {
            _service = service;
        }


        /// <summary>
        ///     Returns the full set of accommodation details.
        /// </summary>
        /// <param name="accommodationId">Accommodation ID, obtained from an availability query.</param>
        /// <returns></returns>
        [HttpGet("accommodations/{accommodationId}")]
        [ProducesResponseType(typeof(AccommodationDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async ValueTask<IActionResult> Get([FromRoute] string accommodationId)
        {
            if (string.IsNullOrWhiteSpace(accommodationId))
                return BadRequest(ProblemDetailsBuilder.Build("No accommodation IDs was provided."));

            var (_, isFailure, response, error) = await _service.Get(accommodationId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Returns accommodations available for a booking.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is the "1st step" for availability search. Returns less information to choose accommodation.
        /// </remarks>
        [HttpPost("availabilities/accommodations")]
        [ProducesResponseType(typeof(AvailabilityDetails), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailability([FromBody] AvailabilityRequest request)
        {
            var (_, isFailure, response, error) = await _service.GetAvailable(request, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Returns available agreements for given accommodation and accommodation id.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="accommodationId"></param>
        /// <returns></returns>
        /// <remarks>
        /// This is the "2nd step" for availability search. Returns richer accommodation details with agreements.
        /// </remarks>
        [HttpPost("availabilities/accommodations/{accommodationId}")]
        [ProducesResponseType(typeof(SingleAccommodationAvailabilityDetails), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetAvailabilityForAccommodation([FromBody] SingleAccommodationAvailabilityRequest request, string accommodationId)
        {
            var (_, isFailure, response, error) = await _service.GetAvailable(request.AvailabilityId, accommodationId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Book an accommodation.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("bookings/accommodations")]
        [ProducesResponseType(typeof(BookingDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Book([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, bookingDetails, error) = await _service.Book(request, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }


        /// <summary>
        ///     Modifies booking.
        /// </summary>
        /// <param name="bookingId"></param>
        /// <param name="request">Modified booking data</param>
        /// <returns>New booking data</returns>
        [HttpPost("bookings/accommodations/{bookingId}/modify")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ModifyBooking(int bookingId, BookingModifiedData request)
        {
            var (_, isFailure, bookingDetails, error) = await _service.ModifyBooking(bookingId, request, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }


        /// <summary>
        ///     Cancel accommodation booking.
        /// </summary>
        /// <param name="bookingId">Id of booking to cancel</param>
        /// <returns></returns>
        [HttpPost("bookings/accommodations/{bookingId}/cancel")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var (_, isFailure, _, error) = await _service.CancelBooking(bookingId);
            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        /// Gets booking data by a booking Id.
        /// </summary>
        /// <returns>Full booking data.</returns>
        [HttpGet("bookings/accommodations/{bookingId}/id")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var (_, isFailure, bookingData, error) = await _service.GetBooking(bookingId);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        /// Gets booking data by reference code.
        /// </summary>
        /// <returns>Full booking data.</returns>
        [HttpGet("bookings/accommodations/{referenceCode}/refcode")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingByReferenceCode(string referenceCode)
        {
            var (_, isFailure, bookingData, error) = await _service.GetBooking(referenceCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        /// Gets all bookings for a current customer.
        /// </summary>
        /// <returns>List of slim booking data.</returns>
        [ProducesResponseType(typeof(List<SlimAccommodationBookingInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpGet("bookings/accommodations/customer")]
        public async Task<IActionResult> GetCustomerBookings()
        {
            var (_, isFailure, bookingData, error) = await _service.GetCustomerBookings();
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        ///     The route returns the availability information from the cache before the booking request.
        /// </summary>
        /// <param name="availabilityId">Cached availability id.</param>
        /// <param name="agreementId">Cached agreement id, e.g. 0f8fad5b-d9cb-469f-a165-70867728950e.</param>
        /// <returns></returns>
        [HttpGet("availabilities/{availabilityId}/{agreementId}")]
        [ProducesResponseType(typeof(BookingAvailabilityInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetBookingAvailability([FromRoute] int availabilityId, [FromRoute] Guid agreementId)
        {
            var (_, isFailure, availabilityInfo, error) = await _service.GetBookingAvailability(availabilityId, agreementId, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(availabilityInfo);
        }


        private readonly IAccommodationService _service;
    }
}