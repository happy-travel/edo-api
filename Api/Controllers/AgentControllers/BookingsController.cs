using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution.Flows;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Models;
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/accommodations/bookings")]
    [Produces("application/json")]
    public class BookingsController : BaseController
    {
        public BookingsController(IFinancialAccountBookingFlow financialAccountBookingFlow,
            IBankCreditCardBookingFlow bankCreditCardBookingFlow,
            IAgentContextService agentContextService,
            IBookingManagementService bookingManagementService,
            IBookingRecordsManager bookingRecordsManager,
            IBookingCreditCardPaymentService creditCardPaymentService,
            IBookingInfoService bookingInfoService,
            IDateTimeProvider dateTimeProvider)
        {
            _financialAccountBookingFlow = financialAccountBookingFlow;
            _bankCreditCardBookingFlow = bankCreditCardBookingFlow;
            _agentContextService = agentContextService;
            _bookingManagementService = bookingManagementService;
            _bookingRecordsManager = bookingRecordsManager;
            _creditCardPaymentService = creditCardPaymentService;
            _bookingInfoService = bookingInfoService;
            _dateTimeProvider = dateTimeProvider;
        }


        /// <summary>
        ///     Initiates the booking procedure. Creates an empty booking record.
        ///     Must be used before a payment request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> RegisterBooking([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, refCode, error) = await _bankCreditCardBookingFlow.Register(request, await _agentContextService.GetAgent(), LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(refCode);
        }


        /// <summary>
        ///     Creates booking in one step. An account will be used for payment.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("book-by-account")]
        [ProducesResponseType(typeof(string), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> Book([FromBody] AccommodationBookingRequest request)
        {
            var (_, isFailure, refCode, error) = await _financialAccountBookingFlow.BookByAccount(request, await _agentContextService.GetAgent(),
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
        [HttpPost("{referenceCode}/finalize")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> FinalizeBooking([FromRoute] string referenceCode)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, bookingDetails, error) = await _bankCreditCardBookingFlow.Finalize(referenceCode, agent, LanguageCode);
            if (isFailure)
                return BadRequest(error);

            return Ok(bookingDetails);
        }


        /// <summary>
        ///     Sends booking request to a supplier to get refreshed booking details, especially - status.
        /// </summary>
        /// <param name="bookingId">Id of the booking</param>
        /// <returns>Updated booking details.</returns>
        [HttpPost("{bookingId}/refresh-status")]
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
        [HttpPost("{bookingId}/cancel")]
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
        [HttpGet("{bookingId}")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingById(int bookingId)
        {
            var (_, isFailure, bookingData, error) =
                await _bookingInfoService.GetAgentAccommodationBookingInfo(bookingId, await _agentContextService.GetAgent(), LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }


        /// <summary>
        ///     Gets booking data by reference code.
        /// </summary>
        /// <returns>Full booking data.</returns>
        [HttpGet("refcode/{referenceCode}")]
        [ProducesResponseType(typeof(AccommodationBookingInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingByReferenceCode(string referenceCode)
        {
            var (_, isFailure, bookingData, error) =
                await _bookingInfoService.GetAgentAccommodationBookingInfo(referenceCode, await _agentContextService.GetAgent(), LanguageCode);

            if (isFailure)
                return BadRequest(error);

            return Ok(bookingData);
        }
        
        
        /// <summary>
        ///     Pays for account booking using credit card
        /// </summary>
        [HttpPost("refcode/{referenceCode}/pay-with-credit-card")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> PayWithCreditCard(string referenceCode)
        {
            var (_, isFailure, error) = await _creditCardPaymentService.PayForAccountBooking(referenceCode, await _agentContextService.GetAgent());
            
            if (isFailure)
                return BadRequest(error);

            return NoContent();
        }


        /// <summary>
        ///     Gets cancellation penalty for cancelling booking
        /// </summary>
        /// <returns>Amount of penalty</returns>
        [HttpGet("{bookingId}/cancellation-penalty")]
        [ProducesResponseType(typeof(MoneyAmount), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingCancellationPenalty(int bookingId)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, booking, error) =
                await _bookingRecordsManager.GetByAgent(bookingId, agent.AgentId);

            if (isFailure)
                return BadRequest(error);

            return Ok(booking.GetCancellationPenalty(_dateTimeProvider.UtcNow()));
        }

        
        /// <summary>
        ///     Gets all bookings for a current agent.
        /// </summary>
        /// <returns>List of slim booking data.</returns>
        [ProducesResponseType(typeof(List<SlimAccommodationBookingInfo>), (int) HttpStatusCode.OK)]
        [HttpGet("agent")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [AgentRequired]
        [EnableQuery]
        public async Task<ActionResult<IQueryable<SlimAccommodationBookingInfo>>> GetAgentBookings()
        {
            return Ok(_bookingInfoService.GetAgentBookingsInfo(await _agentContextService.GetAgent()));
        }


        /// <summary>
        ///     Gets all bookings for an agency of current agent.
        /// </summary>
        /// <returns>List of slim booking data.</returns>
        [ProducesResponseType(typeof(List<AgentBoundedData<SlimAccommodationBookingInfo>>), (int) HttpStatusCode.OK)]
        [HttpGet("agency")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions((InAgencyPermissions.AgencyBookingsManagement))]
        [EnableQuery]
        public async Task<ActionResult<IQueryable<AgentBoundedData<SlimAccommodationBookingInfo>>>> GetAgencyBookings()
        {
            return Ok(_bookingInfoService.GetAgencyBookingsInfo(await _agentContextService.GetAgent()));
        }


        private readonly IFinancialAccountBookingFlow _financialAccountBookingFlow;
        private readonly IBankCreditCardBookingFlow _bankCreditCardBookingFlow;
        private readonly IAgentContextService _agentContextService;
        private readonly IBookingManagementService _bookingManagementService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IBookingCreditCardPaymentService _creditCardPaymentService;
        private readonly IBookingInfoService _bookingInfoService;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}