using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Emailing;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Documents;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/accommodations/supporting-documentation")]
    [Produces("application/json")]
    public class BookingSupportingDocumentsController : BaseController
    {
        public BookingSupportingDocumentsController(IBookingDocumentsMailingService documentsMailingService,
            IBookingDocumentsService bookingDocumentsService, IAgentContextService agentContextService)
        {
            _documentsMailingService = documentsMailingService;
            _bookingDocumentsService = bookingDocumentsService;
            _agentContextService = agentContextService;
        }


        /// <summary>
        ///     Sends booking voucher to an email.
        /// </summary>
        /// <param name="bookingId">Id of the booking.</param>
        /// <param name="sendMailRequest">Send mail request.</param>
        /// <returns></returns>
        [HttpPost("{bookingId}/voucher/send")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> SendBookingVoucher([Required] int bookingId, [Required][FromBody] SendBookingDocumentRequest sendMailRequest)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, error) = await _documentsMailingService.SendVoucher(bookingId, sendMailRequest.Email, agent, LanguageCode);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Sends booking invoice to an email.
        /// </summary>
        /// <param name="bookingId">Id of the booking.</param>
        /// <param name="sendMailRequest">Send mail request.</param>
        /// <returns></returns>
        [HttpPost("{bookingId}/invoice/send")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> SendBookingInvoice([Required] int bookingId, [Required][FromBody] SendBookingDocumentRequest sendMailRequest)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, error) = await _documentsMailingService.SendInvoice(bookingId, sendMailRequest.Email, agent.AgentId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Gets booking voucher data.
        /// </summary>
        /// <param name="bookingId">Id of the booking.</param>
        /// <returns>Voucher data,</returns>
        [HttpGet("{bookingId}/voucher")]
        [ProducesResponseType(typeof(BookingVoucherData), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> GetBookingVoucher([Required] int bookingId)
        {
            var agent = await _agentContextService.GetAgent();
            var result = await _bookingDocumentsService.GenerateVoucher(bookingId, agent, LanguageCode);
            return OkOrBadRequest(result);
        }


        /// <summary>
        ///     Gets booking invoice.
        /// </summary>
        /// <param name="bookingId">Id of the booking.</param>
        /// <returns>Invoice data.</returns>
        [HttpGet("{bookingId}/invoice")]
        [ProducesResponseType(typeof((DocumentRegistrationInfo, BookingInvoiceData)), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.AccommodationBooking)]
        public async Task<IActionResult> GetBookingInvoice([Required] int bookingId)
        {
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, document, error) = await _bookingDocumentsService.GetActualInvoice(bookingId, agent.AgentId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            var (regData, invoice) = document;
            return Ok(new BookingDocument<BookingInvoiceData>(regData.Number, regData.Date, invoice));
        }


        private readonly IBookingDocumentsService _bookingDocumentsService;
        private readonly IAgentContextService _agentContextService;
        private readonly IBookingDocumentsMailingService _documentsMailingService;
    }
}