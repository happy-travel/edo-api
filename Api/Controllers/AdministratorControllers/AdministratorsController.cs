using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Management.Administrators;
using HappyTravel.Edo.Api.Services.Management;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/administrators")]
    [Produces("application/json")]
    public class AdministratorsController : BaseController
    {
        public AdministratorsController(IAdministratorContext administratorContext)
        {
            _administratorContext = administratorContext;
        }
        
        /// <summary>
        ///     Gets current administrator information
        /// </summary>
        /// <returns>Current administrator information.</returns>
        [HttpGet("current")]
        [ProducesResponseType(typeof(AdministratorInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetCurrent()
        {
            var (_, isFailure, administrator, _) = await _administratorContext.GetCurrent();
            
            return isFailure
                ? NoContent()
                : Ok(new AdministratorInfo(administrator.Id,
                    administrator.FirstName,
                    administrator.LastName,
                    administrator.Position));
        }
        
        private readonly IAdministratorContext _administratorContext;
    }
}