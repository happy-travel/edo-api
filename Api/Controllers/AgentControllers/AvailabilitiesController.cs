using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Metrics;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection;
using HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.WideAvailabilitySearch;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;
using Deadline = HappyTravel.Edo.Data.Bookings.Deadline;
using RoomContractSetAvailability = HappyTravel.Edo.Api.Models.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/accommodations/availabilities")]
    [Produces("application/json")]
    public class AvailabilitiesController : BaseController
    {
        public AvailabilitiesController(IAgentContextService agentContextService,
            IWideAvailabilitySearchService wideAvailabilitySearchService,
            IRoomSelectionService roomSelectionService,
            IBookingEvaluationService bookingEvaluationService,
            IDeadlineService deadlineService) 
        {
            _agentContextService = agentContextService;
            _wideAvailabilitySearchService = wideAvailabilitySearchService;
            _roomSelectionService = roomSelectionService;
            _bookingEvaluationService = bookingEvaluationService;
            _deadlineService = deadlineService;
        }
        
        
        /// <summary>
        ///     Starts availability search and returns an identifier to fetch results later
        /// </summary>
        /// <param name="request">Availability request</param>
        /// <returns>Search id</returns>
        [HttpPost("searches")]
        [ProducesResponseType(typeof(Guid), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> StartAvailabilitySearch([FromBody] AvailabilityRequest request)
        {
            Counters.WideAccommodationAvailabilitySearchTimes.Inc();
            
            var agent = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _wideAvailabilitySearchService.StartSearch(request, agent, LanguageCode));
        }


        /// <summary>
        /// Gets state of previous started availability search.
        /// </summary>
        /// <param name="searchId">Search id</param>
        /// <returns>Search state</returns>
        [HttpGet("searches/{searchId}/state")]
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
        [HttpGet("searches/{searchId}")]
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
        [HttpGet("searches/{searchId}/results/{resultId}/state")]
        [ProducesResponseType(typeof(AvailabilitySearchTaskState), (int) HttpStatusCode.OK)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetSearchStateForAccommodationAvailability([FromRoute] Guid searchId, [FromRoute] Guid resultId)
        {
            var (_, isFailure, response, error) = await _roomSelectionService.GetState(searchId, resultId, await _agentContextService.GetAgent());
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

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
        [HttpGet("searches/{searchId}/results/{resultId}")]
        [ProducesResponseType(typeof(IEnumerable<RoomContractSet>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetAvailabilityForAccommodation([FromRoute] Guid searchId, [FromRoute] Guid resultId)
        {
            Counters.AccommodationAvailabilitySearchTimes.Inc();
            
            var (_, isFailure, response, error) = await _roomSelectionService.Get(searchId, resultId, await _agentContextService.GetAgent(), LanguageCode);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

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
        [HttpGet("searches/{searchId}/results/{resultId}/accommodation")]
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
        [HttpGet("searches/{searchId}/results/{resultId}/room-contract-sets/{roomContractSetId}")]
        [ProducesResponseType(typeof(RoomContractSetAvailability?), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.ReadOnly)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationAvailabilitySearch)]
        public async Task<IActionResult> GetExactAvailability([FromRoute] Guid searchId, [FromRoute] Guid resultId, [FromRoute] Guid roomContractSetId)
        {
            var (_, isFailure, availabilityInfo, error) = await _bookingEvaluationService.GetExactAvailability(searchId, resultId, roomContractSetId, await _agentContextService.GetAgent(), LanguageCode);

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
        [HttpGet("searches/{searchId}/results/{resultId}/room-contract-sets/{roomContractSetId}/deadline")]
        [ProducesResponseType(typeof(Deadline), (int) HttpStatusCode.OK)]
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
        
        
        private readonly IAgentContextService _agentContextService;
        private readonly IWideAvailabilitySearchService _wideAvailabilitySearchService;
        private readonly IRoomSelectionService _roomSelectionService;
        private readonly IBookingEvaluationService _bookingEvaluationService;
        private readonly IDeadlineService _deadlineService;
    }
}