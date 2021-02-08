using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin")]
    [Produces("application/json")]
    public class BookingsController : BaseController
    {
        public BookingsController(IAdministratorContext administratorContext,
            IBookingService bookingService,
            IAdministratorBookingManagementService bookingManagementService)
        {
            _administratorContext = administratorContext;
            _bookingService = bookingService;
            _bookingManagementService = bookingManagementService;
        }


        /// <summary>
        ///     Gets a list of all bookings made by the agency
        /// </summary>
        /// <param name="agencyId">Agency Id</param>
        /// <returns>List of bookings</returns>
        [HttpGet("agencies/{agencyId}/bookings")]
        [ProducesResponseType(typeof(List<Booking>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.AgentManagement)]
        public async Task<IActionResult> GetAgencyBookings([FromRoute] int agencyId)
        {
            var (_, isFailure, bookings, error) = await _bookingService.GetAgencyBookings(agencyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(bookings);
        }


        /// <summary>
        ///     Gets a list of all bookings made by the counterparty
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id</param>
        /// <returns>List of bookings</returns>
        [HttpGet("counterparties/{counterpartyId}/bookings")]
        [ProducesResponseType(typeof(List<Booking>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.AgentManagement)]
        public async Task<IActionResult> GetCounterpartyBookings([FromRoute] int counterpartyId)
        {
            var (_, isFailure, bookings, error) = await _bookingService.GetCounterpartyBookings(counterpartyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(bookings);
        }


        /// <summary>
        ///     Gets a list of all bookings made by the agent
        /// </summary>
        /// <param name="agentId">Agent Id</param>
        /// <returns>List of bookings</returns>
        [HttpGet("agents/{agentId}/bookings")]
        [ProducesResponseType(typeof(List<Booking>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.AgentManagement)]
        public async Task<IActionResult> GetAgentBookings([FromRoute] int agentId)
        {
            var (_, isFailure, bookings, error) = await _bookingService.GetAgentBookings(agentId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(bookings);
        }


        /// <summary>
        ///     Cancels accommodation booking by admin.
        /// </summary>
        /// <param name="bookingId">Id of booking to cancel</param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings/{bookingId}/discard")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.BookingManagement)]
        public async Task<IActionResult> Discard(int bookingId)
        {
            var (_, _, admin, _) = await _administratorContext.GetCurrent();
            var (_, isFailure, error) = await _bookingManagementService.Discard(bookingId, admin);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        } 
        
        
        /// <summary>
        ///     Cancel accommodation booking by admin.
        /// </summary>
        /// <param name="bookingId">Id of booking to cancel</param>
        /// <param name="requireSupplierConfirmation">If a supplier returns an error after cancellation request, this is ignored as if it was a success</param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings/{bookingId}/cancel")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.BookingManagement)]
        public async Task<IActionResult> Cancel(int bookingId, [FromQuery] bool requireSupplierConfirmation = true)
        {
            var (_, _, admin, _) = await _administratorContext.GetCurrent();
            var (_, isFailure, error) = await _bookingManagementService.Cancel(bookingId, admin, requireSupplierConfirmation);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }
        
        
        private readonly IAdministratorContext _administratorContext;
        private readonly IBookingService _bookingService;
        private readonly IAdministratorBookingManagementService _bookingManagementService;
    }
}