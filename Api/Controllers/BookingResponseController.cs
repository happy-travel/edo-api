using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Http;
using HappyTravel.Edo.Api.Services.ProviderResponses;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class BookingResponseController : BaseController
    {
        public BookingResponseController(INetstormingResponseService netstormingResponseService, 
            IBookingWebhookResponseService bookingWebhookResponseService,
            RequestMetadataProvider requestMetadataProvider)
        {
            _netstormingResponseService = netstormingResponseService;
            _bookingWebhookResponseService = bookingWebhookResponseService;
            _requestMetadataProvider = requestMetadataProvider;
        }
        
        
        /// <summary>
        /// Netstorming sends XML responses with booking details on this route.
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [HttpPost("bookings/accommodations/responses/netstorming")]
        public async Task<IActionResult> HandleNetstormingBookingResponse()
        {
            var (_, isXmlRequestFailure, xmlRequestData, xmlRequestError) = await RequestHelper.GetAsBytes(HttpContext.Request.Body);
            if (isXmlRequestFailure)
                return BadRequest(new ProblemDetails
                {
                    Detail = xmlRequestError,
                    Status = (int) HttpStatusCode.BadRequest
                });
            
            var (_, isFailure, error) = await _netstormingResponseService.ProcessBookingDetailsResponse(xmlRequestData, _requestMetadataProvider.Get());
            if (isFailure)
                return BadRequest(error);
            
            return Ok();
        }


        [AllowAnonymous]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType((int) HttpStatusCode.BadRequest)]
        [HttpPost("bookings/accommodations/responses/etg")]
        public async Task<IActionResult> HandleEtgBookingResponse()
        {
            var (_, isFailure, error) = await _bookingWebhookResponseService.ProcessBookingData(HttpContext.Request.Body, DataProviders.Etg, _requestMetadataProvider.Get());
            return Ok(isFailure ? error : "ok");
        }


        private readonly IBookingWebhookResponseService _bookingWebhookResponseService;
        private readonly RequestMetadataProvider _requestMetadataProvider;
        private readonly INetstormingResponseService _netstormingResponseService;
    }
}