using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InCounterpartyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AccommodationsController : BaseController
    {
        public AccommodationsController(IAccommodationService service, 
            IAvailabilityService availabilityService,
            IBookingService bookingService,
            IBookingRecordsManager bookingRecordsManager,
            RequestMetadataProvider requestMetadataProvider,
            IAgentContext agentContext)
        {
            _service = service;
            _availabilityService = availabilityService;
            _bookingService = bookingService;
            _bookingRecordsManager = bookingRecordsManager;
            _requestMetadataProvider = requestMetadataProvider;
            _agentContext = agentContext;
        }


        /// <summary>
        ///     Returns the full set of accommodation details.
        /// </summary>
        /// <param name="source">Accommodation source from search results.</param>
        /// <param name="accommodationId">Accommodation ID, obtained from an availability query.</param>
        /// <returns></returns>
        [HttpGet("{source}/accommodations/{accommodationId}")]
        [ProducesResponseType(typeof(AccommodationDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async ValueTask<IActionResult> Get([FromRoute] DataProviders source, [FromRoute] string accommodationId)
        {
            if (string.IsNullOrWhiteSpace(accommodationId))
                return BadRequest(ProblemDetailsBuilder.Build("No accommodation IDs was provided."));

            var (_, isFailure, response, error) = await _service.Get(source, accommodationId, _requestMetadataProvider.Get());
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
        ///     This is the "1st step" for availability search. Returns less information to choose accommodation.
        /// </remarks>
        [HttpPost("availabilities/accommodations")]
        [ProducesResponseType(typeof(CombinedAvailabilityDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetAvailability([FromBody] AvailabilityRequest request)
        {
            var agent = await _agentContext.GetAgent();
            var (_, isFailure, response, error) = await _availabilityService.GetAvailable(request, agent, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Returns available room contract sets for given accommodation and accommodation id.
        /// </summary>
        /// <param name="availabilityId">Availability id from 1-st step results.</param>
        /// <param name="source">Availability source from 1-st step results.</param>
        /// <param name="accommodationId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     This is the "2nd step" for availability search. Returns richer accommodation details with room contract sets.
        /// </remarks>
        [HttpPost("{source}/accommodations/{accommodationId}/availabilities/{availabilityId}")]
        [ProducesResponseType(typeof(SingleAccommodationAvailabilityDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetAvailabilityForAccommodation([FromRoute] DataProviders source, [FromRoute] string accommodationId, [FromRoute]  string availabilityId)
        {
            var (_, isFailure, response, error) = await _availabilityService.GetAvailable(source, accommodationId, availabilityId, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     The last 3rd search step before the booking request. Uses the exact search.
        /// </summary>
        /// <param name="source">Availability source from 1-st step results.</param>
        /// <param name="availabilityId">Availability id from the previous step</param>
        /// <param name="roomContractSetId">Room contract set id from the previous step</param>
        /// <returns></returns>
        [HttpPost("{source}/accommodations/availabilities/{availabilityId}/room-contract-sets/{roomContractSetId}")]
        [ProducesResponseType(typeof(SingleAccommodationAvailabilityDetailsWithDeadline), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetExactAvailability([FromRoute] DataProviders source, [FromRoute] string availabilityId, [FromRoute] Guid roomContractSetId)
        {
            var (_, isFailure, availabilityInfo, error) = await _availabilityService.GetExactAvailability(source, availabilityId, roomContractSetId, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(availabilityInfo);
        }
        
        
        /// <summary>
        ///     Gets deadline details for given room contract set.
        /// </summary>
        /// <param name="source">Availability source.</param>
        /// <param name="availabilityId">Availability id for room contract set</param>
        /// <param name="roomContractSetId">Selected room contract set id</param>
        /// <returns></returns>
        [HttpGet("{source}/accommodations/availabilities/{availabilityId}/room-contract-sets/{roomContractSetId}/deadline")]
        [ProducesResponseType(typeof(DeadlineDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetDeadline([FromRoute] DataProviders source, [FromRoute] string availabilityId, [FromRoute] Guid roomContractSetId)
        {
            var (_, isFailure, deadline, error) = await _availabilityService.GetDeadlineDetails(source, availabilityId, roomContractSetId, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(deadline);
        }
        

        /// <summary>
        ///     Initiates the booking procedure. Creates an empty booking record.
        ///     Must be used before a payment request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationBooking)]
        public async Task<IActionResult> RegisterBooking([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, refCode, error) = await _bookingService.Register(request, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(refCode);
        }
        
        
        /// <summary>
        ///     Sends booking request to a data provider and finalize the booking procedure.
        ///     Must be used after a successful payment request.
        /// </summary>
        /// <param name="referenceCode"></param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings/{referenceCode}/finalize")]
        [ProducesResponseType(typeof(BookingDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationBooking)]
        public async Task<IActionResult> FinalizeBooking([FromRoute] string referenceCode)
        {
            var (_, isFailure, bookingDetails, error) = await _bookingService.Finalize(referenceCode, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }


        /// <summary>
        ///     Sends booking request to a data provider to get refreshed booking details, especially - status.
        /// </summary>
        /// <param name="bookingId">Id of the booking</param>
        /// <returns>Updated booking details.</returns>
        [HttpPost("accommodations/bookings/{bookingId}/refresh-status")]
        [ProducesResponseType(typeof(BookingDetails), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationBooking)]
        public async Task<IActionResult> RefreshStatus([FromRoute] int bookingId)
        {
            var (_, isFailure, bookingDetails, error) = await _bookingService.RefreshStatus(bookingId, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }

        
        /// <summary>
        ///     Cancel accommodation booking.
        /// </summary>
        /// <param name="bookingId">Id of booking to cancel</param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings/{bookingId}/cancel")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InCounterpartyPermissions(InCounterpartyPermissions.AccommodationBooking)]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var (_, isFailure, error) = await _bookingService.Cancel(bookingId, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        ///     Gets booking data by a booking Id.
        /// </summary>
        /// <returns>Full booking data.</returns>
        [HttpGet("accommodations/bookings/{bookingId}")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var (_, isFailure, bookingData, error) = await _bookingRecordsManager.GetAgentBookingInfo(bookingId);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        ///     Gets booking data by reference code.
        /// </summary>
        /// <returns>Full booking data.</returns>
        [HttpGet("accommodations/bookings/refcode/{referenceCode}")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingByReferenceCode(string referenceCode)
        {
            var (_, isFailure, bookingData, error) = await _bookingRecordsManager.GetAgentBookingInfo(referenceCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        ///     Gets all bookings for a current agent.
        /// </summary>
        /// <returns>List of slim booking data.</returns>
        [ProducesResponseType(typeof(List<SlimAccommodationBookingInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpGet("accommodations/bookings/agent")]
        [AgentRequired]
        public async Task<IActionResult> GetAgentBookings()
        {
            var (_, isFailure, bookings, error) = await _bookingRecordsManager.GetAgentBookingsInfo();
            if (isFailure)
                return BadRequest(error);

            return Ok(bookings);
        }


        private readonly IAccommodationService _service;
        private readonly IAvailabilityService _availabilityService;
        private readonly IBookingService _bookingService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly RequestMetadataProvider _requestMetadataProvider;
        private readonly IAgentContext _agentContext;
    }
}