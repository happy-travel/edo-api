using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.Money.Models;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;
using RoomContractSetAvailability = HappyTravel.Edo.Api.Models.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class AccommodationsController : BaseController
    {
        public AccommodationsController(IWideAvailabilitySearchService wideAvailabilitySearchService,
            IRoomSelectionService roomSelectionService,
            IBookingEvaluationService bookingEvaluationService,
            IBookingManagementService bookingManagementService,
            IBookingRecordsManager bookingRecordsManager,
            IAgentContextService agentContextService,
            IBookingRegistrationService bookingRegistrationService,
            IDateTimeProvider dateTimeProvider,
            IDeadlineService deadlineService)
        {
            _wideAvailabilitySearchService = wideAvailabilitySearchService;
            _roomSelectionService = roomSelectionService;
            _bookingEvaluationService = bookingEvaluationService;
            _bookingManagementService = bookingManagementService;
            _bookingRecordsManager = bookingRecordsManager;
            _agentContextService = agentContextService;
            _bookingRegistrationService = bookingRegistrationService;
            _dateTimeProvider = dateTimeProvider;
            _deadlineService = deadlineService;
        }


        /// <summary>
        ///     Starts availability search and returns an identifier to fetch results later
        /// </summary>
        /// <param name="request">Availability request</param>
        /// <returns>Search id</returns>
        [HttpPost("accommodations/availabilities/searches")]
        [ProducesResponseType(typeof(Guid), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> StartAvailabilitySearch([FromBody] AvailabilityRequest request)
        {
            var agent = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _wideAvailabilitySearchService.StartSearch(request, agent, LanguageCode));
        }


        /// <summary>
        /// Gets state of previous started availability search.
        /// </summary>
        /// <param name="searchId">Search id</param>
        /// <returns>Search state</returns>
        [HttpGet("accommodations/availabilities/searches/{searchId}/state")]
        [ProducesResponseType(typeof(WideAvailabilitySearchState), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetAvailabilitySearchState([FromRoute] Guid searchId)
        {
            return Ok(await _wideAvailabilitySearchService.GetState(searchId, await _agentContextService.GetAgent()));
        }


        /// <summary>
        /// Gets result of previous started availability search.
        /// </summary>
        /// <param name="searchId">Search id</param>
        /// <returns>Availability results</returns>
        [HttpGet("accommodations/availabilities/searches/{searchId}")]
        [ProducesResponseType(typeof(IEnumerable<WideAvailabilityResult>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        [EnableQuery(MaxAnyAllExpressionDepth = 2, EnsureStableOrdering = false)]
        public async Task<IEnumerable<WideAvailabilityResult>> GetAvailabilitySearchResult([FromRoute] Guid searchId)
        {
            // TODO: Add validation and fool check for skip and top parameters
            return await _wideAvailabilitySearchService.GetResult(searchId, await _agentContextService.GetAgent());
        }


        /// <summary>
        /// Returns available room contract sets for given accommodation and accommodation id.
        /// </summary>
        /// <param name="searchId">Availability search id from the first step</param>
        /// <param name="resultId">Selected result id from the first step</param>
        /// <returns></returns>
        /// <remarks>
        ///     This is the method to get "2nd step" for availability search state.
        /// </remarks>
        [HttpGet("accommodations/availabilities/searches/{searchId}/results/{resultId}/state")]
        [ProducesResponseType(typeof(AvailabilitySearchTaskState), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetSearchStateForAccommodationAvailability([FromRoute] Guid searchId, [FromRoute] Guid resultId)
        {
            var (_, isFailure, response, error) = await _roomSelectionService.GetState(searchId, resultId, await _agentContextService.GetAgent());
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        /// Returns available room contract sets for given accommodation and accommodation id.
        /// </summary>
        /// <param name="searchId">Availability search id from the first step</param>
        /// <param name="resultId">Selected result id from the first step</param>
        /// <returns></returns>
        /// <remarks>
        ///     This is the "2nd step" for availability search. Returns richer accommodation details with room contract sets.
        /// </remarks>
        [HttpGet("accommodations/availabilities/searches/{searchId}/results/{resultId}")]
        [ProducesResponseType(typeof(IEnumerable<RoomContractSet>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetAvailabilityForAccommodation([FromRoute] Guid searchId, [FromRoute] Guid resultId)
        {
            var (_, isFailure, response, error) = await _roomSelectionService.Get(searchId, resultId, await _agentContextService.GetAgent(), LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///  Returns the full set of accommodation details for given availability search result
        /// </summary>
        /// <param name="searchId">Availability search id from the first step</param>
        /// <param name="resultId">Selected result id from the first step</param>
        /// <returns></returns>
        /// <remarks>
        ///     This is accommodation details for "2nd step" for availability search.
        /// </remarks>
        [HttpGet("accommodations/availabilities/searches/{searchId}/results/{resultId}/accommodation")]
        [ProducesResponseType(typeof(Accommodation), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetAccommodation([FromRoute] Guid searchId, [FromRoute] Guid resultId)
        {
            var (_, isFailure, response, error) =
                await _roomSelectionService.GetAccommodation(searchId, resultId, await _agentContextService.GetAgent(), LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     The last 3rd search step before the booking request. Uses the exact search.
        /// </summary>
        /// <param name="searchId">Availability search id from the first step</param>
        /// <param name="resultId">Selected result id from the first step</param>
        /// <param name="roomContractSetId">Room contract set id from the previous step</param>
        /// <returns></returns>
        [HttpGet("accommodations/availabilities/searches/{searchId}/results/{resultId}/room-contract-sets/{roomContractSetId}")]
        [ProducesResponseType(typeof(RoomContractSetAvailability?), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetExactAvailability([FromRoute] Guid searchId, [FromRoute] Guid resultId, [FromRoute] Guid roomContractSetId)
        {
            var (_, isFailure, availabilityInfo, error) = await _bookingEvaluationService.GetExactAvailability(
                searchId,
                resultId,
                roomContractSetId,
                await _agentContextService.GetAgent(), LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(availabilityInfo);
        }


        /// <summary>
        ///     Gets deadline details for given room contract set.
        /// </summary>
        /// <param name="searchId">Availability search id from the first step</param>
        /// <param name="resultId">Selected result id from the first step</param>
        /// <param name="roomContractSetId">Room contract set id from the previous step</param>
        /// <returns></returns>
        [HttpGet("accommodations/availabilities/searches/{searchId}/results/{resultId}/room-contract-sets/{roomContractSetId}/deadline")]
        [ProducesResponseType(typeof(Edo.Data.Booking.Deadline), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetDeadline([FromRoute] Guid searchId, [FromRoute] Guid resultId, [FromRoute] Guid roomContractSetId)
        {
            var (_, isFailure, deadline, error) =
                await _deadlineService.GetDeadlineDetails(searchId, resultId, roomContractSetId, await _agentContextService.GetAgent(), LanguageCode);
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
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> RegisterBooking([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, refCode, error) = await _bookingRegistrationService.Register(request, await _agentContextService.GetAgent(), LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(refCode);
        }


        /// <summary>
        ///     Creates booking in one step. An account will be used for payment.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings/book-by-account")]
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> Book([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, refCode, error) = await _bookingRegistrationService.BookByAccount(request, await _agentContextService.GetAgent(),
                LanguageCode, ClientIp);
            if (isFailure)
                return BadRequest(error);

            return Ok(refCode);
        }


        /// <summary>
        ///     Sends booking request to supplier and finalize the booking procedure.
        ///     Must be used after a successful payment request.
        /// </summary>
        /// <param name="referenceCode"></param>
        /// <returns></returns>
        [HttpPost("accommodations/bookings/{referenceCode}/finalize")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> FinalizeBooking([FromRoute] string referenceCode)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, bookingDetails, error) = await _bookingRegistrationService.Finalize(referenceCode, agent, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }


        /// <summary>
        ///     Sends booking request to a supplier to get refreshed booking details, especially - status.
        /// </summary>
        /// <param name="bookingId">Id of the booking</param>
        /// <returns>Updated booking details.</returns>
        [HttpPost("accommodations/bookings/{bookingId}/refresh-status")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> RefreshStatus([FromRoute] int bookingId)
        {
            var (_, isFailure, _, error) = await _bookingManagementService.RefreshStatus(bookingId);
            if (isFailure)
                return BadRequest(error);

            return Ok();
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
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, _, error) = await _bookingManagementService.Cancel(bookingId, agent);
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
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var (_, isFailure, bookingData, error) =
                await _bookingRecordsManager.GetAgentAccommodationBookingInfo(bookingId, await _agentContextService.GetAgent(), LanguageCode);

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
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingByReferenceCode(string referenceCode)
        {
            var (_, isFailure, bookingData, error) =
                await _bookingRecordsManager.GetAgentAccommodationBookingInfo(referenceCode, await _agentContextService.GetAgent(), LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }

        
        /// <summary>
        ///     Gets cancellation penalty for cancelling booking
        /// </summary>
        /// <returns>Amount of penalty</returns>
        [HttpGet("accommodations/bookings/{bookingId}/cancellation-penalty")]
        [ProducesResponseType(typeof(MoneyAmount), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingCancellationPenalty(int bookingId)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, booking, error) =
                await _bookingRecordsManager.Get(bookingId, agent.AgentId);

            if (isFailure)
                return BadRequest(error);

            return Ok(booking.GetCancellationPenalty(_dateTimeProvider.UtcNow()));
        }
        

        /// <summary>
        ///     Gets all bookings for a current agent.
        /// </summary>
        /// <returns>List of slim booking data.</returns>
        [ProducesResponseType(typeof(List<SlimAccommodationBookingInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpGet("accommodations/bookings/agent")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetAgentBookings()
        {
            var (_, isFailure, bookings, error) = await _bookingRecordsManager.GetAgentBookingsInfo(await _agentContextService.GetAgent());
            if (isFailure)
                return BadRequest(error);

            return Ok(bookings);
        }

        
        /// <summary>
        ///     Gets all bookings for an agency of current agent.
        /// </summary>
        /// <returns>List of slim booking data.</returns>
        [ProducesResponseType(typeof(List<AgentBoundedData<SlimAccommodationBookingInfo>>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpGet("accommodations/bookings/agency")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions((InAgencyPermissions.AgencyBookingsManagement))]
        public async Task<IActionResult> GetAgencyBookings()
        {
            var bookings = await _bookingRecordsManager.GetAgencyBookingsInfo(await _agentContextService.GetAgent());
            return Ok(bookings);
        }
        

        private readonly IDeadlineService _deadlineService;
        private readonly IWideAvailabilitySearchService _wideAvailabilitySearchService;
        private readonly IRoomSelectionService _roomSelectionService;
        private readonly IBookingEvaluationService _bookingEvaluationService;
        private readonly IBookingManagementService _bookingManagementService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IAgentContextService _agentContextService;
        private readonly IBookingRegistrationService _bookingRegistrationService;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}