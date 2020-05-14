using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Emailing;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Mailing;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/accommodations/supporting-documentation")]
    [Produces("application/json")]
    public class BookingSupportingDocumentsController : BaseController
    {
        public BookingSupportingDocumentsController(IBookingMailingService bookingMailingService,
            IBookingDocumentsService bookingDocumentsService,
            RequestMetadataProvider requestMetadataProvider)
        {
            _bookingMailingService = bookingMailingService;
            _bookingDocumentsService = bookingDocumentsService;
            _requestMetadataProvider = requestMetadataProvider;
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
        public async Task<IActionResult> SendBookingVoucher([Required] int bookingId, [Required][FromBody] SendBookingDocumentRequest sendMailRequest)
        {
            var (_, isFailure, error) = await _bookingMailingService.SendVoucher(bookingId, sendMailRequest.Email, _requestMetadataProvider.Get());
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
        public async Task<IActionResult> SendBookingInvoice([Required] int bookingId, [Required][FromBody] SendBookingDocumentRequest sendMailRequest)
        {
            var (_, isFailure, error) = await _bookingMailingService.SendInvoice(bookingId, sendMailRequest.Email, LanguageCode);
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
        [AgentRequired]
        public async Task<IActionResult> GetBookingVoucher([Required] int bookingId)
        {
            var result = await _bookingDocumentsService.GenerateVoucher(bookingId, _requestMetadataProvider.Get());
            return OkOrBadRequest(result);
        }


        /// <summary>
        ///     Gets booking invoice.
        /// </summary>
        /// <param name="bookingId">Id of the booking.</param>
        /// <returns>Invoice data.</returns>
        [HttpGet("{bookingId}/invoice")]
        [ProducesResponseType(typeof(BookingInvoiceData), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async Task<IActionResult> GetBookingInvoice([Required] int bookingId)
        {
            var result = await _bookingDocumentsService.GenerateInvoice(bookingId, LanguageCode);
            return OkOrBadRequest(result);
        }


        private readonly IBookingDocumentsService _bookingDocumentsService;
        private readonly RequestMetadataProvider _requestMetadataProvider;
        private readonly IBookingMailingService _bookingMailingService;
    }
}