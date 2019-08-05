﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Locations;
using HappyTravel.Edo.Api.Services.Locations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/locations")]
    [Produces("application/json")]
    public class LocationsController : BaseController
    {
        public LocationsController(ILocationService service)
        {
            _service = service;
        }


        /// <summary>
        ///     Returns a list of world countries.
        /// </summary>
        /// <param name="query">The search query text.</param>
        /// <returns></returns>
        [HttpGet("countries")]
        [ProducesResponseType(typeof(List<Country>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetCountries([FromQuery] string query)
            => Ok(await _service.GetCountries(query, LanguageCode));


        /// <summary>
        ///     Returns location predictions what a used when searching
        /// </summary>
        /// <param name="query">The search query text.</param>
        /// <param name="sessionId">The search session ID.</param>
        /// <returns></returns>
        [HttpGet("predictions")]
        [ProducesResponseType(typeof(List<Prediction>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetLocationPredictions([FromQuery] string query, [FromQuery] [Required] string sessionId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest(ProblemDetailsBuilder.Build($"'{nameof(query)}' is required."));

            var (_, isFailure, value, error) = await _service.GetPredictions(query, sessionId, LanguageCode);
            return isFailure
                ? (IActionResult) BadRequest(error)
                : Ok(value);
        }


        /// <summary>
        ///     Returns a list of world regions.
        /// </summary>
        /// <returns></returns>
        [HttpGet("regions")]
        [ProducesResponseType(typeof(List<Region>), (int) HttpStatusCode.OK)]
        public async Task<IActionResult> GetRegions() => Ok(await _service.GetRegions(LanguageCode));


        /// <summary>
        ///     Internal. Sets locations, gathered from booking sources, to make predictions.
        /// </summary>
        /// <param name="locations"></param>
        /// <returns></returns>
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [HttpPost]
        public async ValueTask<IActionResult> SetPredictions([FromBody] IEnumerable<Location> locations)
        {
            if (locations is null || !locations.Any())
                return NoContent();

            await _service.Set(locations);
            return NoContent();
        }


        private readonly ILocationService _service;
    }
}