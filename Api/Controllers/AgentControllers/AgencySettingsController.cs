using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/agencies")]
    [Produces("application/json")]
    public class AgencySettingsController : BaseController
    {
        public AgencySettingsController(IAgentContextService agentContextService,
            IAgencySystemSettingsService agencySystemSettingsService,
            IAccommodationBookingSettingsService accommodationBookingSettingsService)
        {
            _agentContextService = agentContextService;
            _agencySystemSettingsService = agencySystemSettingsService;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
        }


        /// <summary>
        ///     Gets Advanced Purchase Rates settings for an agency.
        /// </summary>
        /// <param name="agencyId">The ID of an agency to get settings for</param>
        /// <returns></returns>
        [HttpGet("{agencyId}/system-settings/apr-settings")]
        [ProducesResponseType(typeof(AprMode), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAdvancedPurchaseRatesSettings([FromRoute] int agencyId)
        {
            var agent = await _agentContextService.GetAgent();
            return Ok((await _accommodationBookingSettingsService.Get(agent)).AprMode);
        }


        /// <summary>
        ///     Gets setting which tells what payment methods to show for booking payment.
        /// </summary>
        /// <param name="agencyId">Id of an agency to get settings for</param>
        /// <returns>Displayed payment methods setting</returns>
        [HttpGet("{agencyId}/system-settings/displayed-payment-options")]
        [ProducesResponseType(typeof(DisplayedPaymentOptionsSettings), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetDisplayedPaymentOptions([FromRoute] int agencyId)
        {
            // TODO: Remove agencyId from route NIJO-1075
            var agent = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _agencySystemSettingsService.GetDisplayedPaymentOptions(agent));
        }


        private readonly IAgentContextService _agentContextService;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IAgencySystemSettingsService _agencySystemSettingsService;
    }
}
