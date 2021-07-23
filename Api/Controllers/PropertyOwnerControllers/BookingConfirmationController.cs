using HappyTravel.Edo.Api.Models.PropertyOwners;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.PropertyOwners;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.Controllers.PropertyOwnerControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/property-owner/confirmations")]
    [Produces("application/json")]
    [Authorize]
    public class BookingConfirmationController : BaseController
    {
        public BookingConfirmationController(IBookingConfirmationService bookingConfirmationService, IBookingInfoService bookingInfoService, 
            IPropertyOwnerConfirmationUrlGenerator urlGenerationService)
        {
            _bookingConfirmationService = bookingConfirmationService;
            _bookingInfoService = bookingInfoService;
            _urlGenerationService = urlGenerationService;
        }


        /// <summary>
        ///     Gets an actual booking status and confirmation code
        /// </summary>
        /// <param name="encryptedReferenceCode">Booking reference code</param>
        /// <returns>Booking status and property owner confirmation code</returns>
        [HttpGet("{encryptedReferenceCode}")]
        [ProducesResponseType(typeof(SlimBookingConfirmation), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromRoute] string encryptedReferenceCode)
        {
            var referenceCode = _urlGenerationService.ReadReferenceCode(encryptedReferenceCode);

            return OkOrBadRequest(await _bookingConfirmationService.Get(referenceCode));
        }


        /// <summary>
        ///     Gets booking confirmation changes history
        /// </summary>
        /// <param name="encryptedReferenceCode">Booking reference code for retrieving confirmation change history</param>
        /// <returns>List of booking confirmation change events</returns>
        [HttpGet("{encryptedReferenceCode}/confirmation-history")]
        [ProducesResponseType(typeof(List<BookingConfirmationHistoryEntry>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> GetBookingConfirmationCodeHistory([FromRoute] string encryptedReferenceCode)
        {
            var referenceCode = _urlGenerationService.ReadReferenceCode(encryptedReferenceCode);

            return OkOrBadRequest(await _bookingInfoService.GetBookingConfirmationHistory(referenceCode));
        }


        /// <summary>
        ///     Updates booking status and hotel confirmation code
        /// </summary>
        /// <param name="encryptedReferenceCode">Booking reference code</param>
        /// <param name="bookingConfirmation">Booking confirmation data</param>
        /// <returns></returns>
        [HttpPut("{encryptedReferenceCode}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> Update([FromRoute] string encryptedReferenceCode, [FromBody] BookingConfirmation bookingConfirmation)
        {
            var referenceCode = _urlGenerationService.ReadReferenceCode(encryptedReferenceCode);

            return NoContentOrBadRequest(await _bookingConfirmationService.Update(referenceCode, bookingConfirmation));
        }
        
        
        private readonly IBookingConfirmationService _bookingConfirmationService;
        private readonly IBookingInfoService _bookingInfoService;
        private readonly IPropertyOwnerConfirmationUrlGenerator _urlGenerationService;
    }
}
