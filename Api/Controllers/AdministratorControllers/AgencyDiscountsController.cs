using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.AdministratorServices.Models;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Management.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/agencies/{agencyId:int}/discounts")]
    [Produces("application/json")]
    public class AgencyDiscountsController : BaseController
    {
        public AgencyDiscountsController(IAgencyDiscountManagementService discountManagementService)
        {
            _discountManagementService = discountManagementService;
        }


        /// <summary>
        /// Gets discounts for an agency.
        /// </summary>
        /// <param name="agencyId">Agency id</param>
        /// <returns>List of discounts</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<DiscountInfo>), StatusCodes.Status200OK)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> GetAgencyDiscounts([FromRoute] int agencyId)
        {
            return Ok(await _discountManagementService.Get(agencyId));
        }


        /// <summary>
        /// Creates discount
        /// </summary>
        /// <param name="agencyId">Agency id</param>
        /// <param name="createDiscountRequest">Discount settings</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> AddDiscount([FromRoute] int agencyId, [FromBody] CreateDiscountRequest createDiscountRequest)
        {
            var (_, isFailure, error) = await _discountManagementService.Add(agencyId, createDiscountRequest);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Deactivates discount
        /// </summary>
        /// <param name="agencyId">Agency id</param>
        /// <param name="discountId">Id of the discount to deactivate.</param>
        /// <returns></returns>
        [HttpPost("{discountId:int}/deactivate")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> StopDiscount([FromRoute] int agencyId, [FromRoute] int discountId)
        {
            var (_, isFailure, error) = await _discountManagementService.Stop(agencyId, discountId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Activates discount
        /// </summary>
        /// <param name="agencyId">Agency id</param>
        /// <param name="discountId">Id of the discount to deactivate.</param>
        /// <returns></returns>
        [HttpPost("{discountId:int}/activate")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> StartDiscount([FromRoute] int agencyId, [FromRoute] int discountId)
        {
            var (_, isFailure, error) = await _discountManagementService.Start(agencyId, discountId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Updates discount settings.
        /// </summary>
        /// <param name="agencyId">Agency id</param>
        /// <param name="discountId">Id of the discount.</param>
        /// <param name="editDiscountRequest">Updated settings.</param>
        /// <returns></returns>
        [HttpPut("{discountId:int}")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AdministratorPermissions(AdministratorPermissions.MarkupManagement)]
        public async Task<IActionResult> UpdateDiscount(int agencyId, int discountId, [FromBody] EditDiscountRequest editDiscountRequest)
        {
            var (_, isFailure, error) = await _discountManagementService.Update(agencyId, discountId, editDiscountRequest);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        private readonly IAgencyDiscountManagementService _discountManagementService;
    }
}