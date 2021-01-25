using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/locations")]
    [Produces("application/json")]
    public class LocationsController : BaseController
    {
        public LocationsController(IAgentContextService agentContextService, ILocationService service)
        {
            _agentContextService = agentContextService;
            _service = service;
        }


        /// <summary>
        ///     Returns a list of world countries.
        /// </summary>
        /// <param name="query">The search query text.</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("countries")]
        [ProducesResponseType(typeof(List<Country>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetCountries([FromQuery] string query) => Ok(await _service.GetCountries(query, LanguageCode));


        /// <summary>
        ///     Returns location predictions what a used when searching
        /// </summary>
        /// <param name="query">The search query text.</param>
        /// <param name="sessionId">The search session ID.</param>
        /// <returns></returns>
        [HttpGet("predictions")]
        [ProducesResponseType(typeof(List<Prediction>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        public async Task<IActionResult> GetLocationPredictions([FromQuery] string query, [FromQuery] [Required] string sessionId)
        {
            var agent = await _agentContextService.GetAgent();

            //TODO: remove agent ID check when locality restriction will be removed (NIJO-345)
            var (_, isFailure, value, error) = await _service.GetPredictions(query, sessionId, agent, LanguageCode);
            return isFailure
                ? (IActionResult) BadRequest(error)
                : Ok(value);
        }


        /// <summary>
        ///     Returns a list of world regions.
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("regions")]
        [ProducesResponseType(typeof(List<Region>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetRegions() => Ok(await _service.GetRegions(LanguageCode));


        private readonly IAgentContextService _agentContextService;
        private readonly ILocationService _service;
    }
}