using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Files;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/agencies")]
    [Produces("application/json")]
    public class AgencyImagesController : BaseController
    {
        public AgencyImagesController(IImageFileService imageFileService,
            IAgentContextService agentContextService)
        {
            _imageFileService = imageFileService;
            _agentContextService = agentContextService;
        }


        /// <summary>
        ///     Uploads an image for banner
        /// </summary>
        [HttpPut("banner")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyImagesManagement)]
        public async Task<IActionResult> SetBanner([FromForm] IFormFile file)
        {
            var agentContext = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _imageFileService.SetBanner(file, agentContext));
        }


        /// <summary>
        ///     Uploads an image for logo
        /// </summary>
        [HttpPut("logo")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyImagesManagement)]
        public async Task<IActionResult> SetLogo([FromForm] IFormFile file)
        {
            var agentContext = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _imageFileService.SetLogo(file, agentContext));
        }


        /// <summary>
        ///     Gets an image for banner
        /// </summary>
        [HttpGet("banner")]
        [ProducesResponseType(typeof(SlimUploadedImage), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyImagesManagement)]
        public async Task<IActionResult> GetBanner()
        {
            var agentContext = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _imageFileService.GetBanner(agentContext));
        }


        /// <summary>
        ///     Gets an image for logo
        /// </summary>
        [HttpGet("logo")]
        [ProducesResponseType(typeof(SlimUploadedImage), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyImagesManagement)]
        public async Task<IActionResult> GetLogo()
        {
            var agentContext = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _imageFileService.GetLogo(agentContext));
        }


        /// <summary>
        ///     Deletes the image for banner
        /// </summary>
        [HttpDelete("banner")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyImagesManagement)]
        public async Task<IActionResult> DeleteBanner()
        {
            var agentContext = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _imageFileService.DeleteBanner(agentContext));
        }


        /// <summary>
        ///     Deletes the image for logo
        /// </summary>
        [HttpDelete("logo")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyImagesManagement)]
        public async Task<IActionResult> DeleteLogo()
        {
            var agentContext = await _agentContextService.GetAgent();
            return OkOrBadRequest(await _imageFileService.DeleteLogo(agentContext));
        }


        private readonly IImageFileService _imageFileService;
        private readonly IAgentContextService _agentContextService;
    }
}
