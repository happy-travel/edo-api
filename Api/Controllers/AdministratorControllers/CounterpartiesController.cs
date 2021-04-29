using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.AdministratorServices;
using HappyTravel.Edo.Api.AdministratorServices.Models;
using HappyTravel.Edo.Api.Services.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin/counterparties")]
    [Produces("application/json")]
    public class CounterpartiesController : BaseController
    {
        public CounterpartiesController(ICounterpartyManagementService counterpartyManagementService,
            IContractFileManagementService contractFileManagementService,
            ICounterpartyVerificationService counterpartyVerificationService)
        {
            _counterpartyManagementService = counterpartyManagementService;
            _contractFileManagementService = contractFileManagementService;
            _counterpartyVerificationService = counterpartyVerificationService;
        }


        /// <summary>
        /// Gets specified counterparty.
        /// </summary>
        /// <param name="counterpartyId">Id of counterparty to get</param>
        /// <returns></returns>
        [HttpGet("{counterpartyId}")]
        [ProducesResponseType(typeof(List<CounterpartyInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> Get(int counterpartyId)
        {
            var (_, isFailure, counterparties, error) = await _counterpartyManagementService.Get(counterpartyId);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(counterparties);
        }


        /// <summary>
        /// Gets all counterparties
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<CounterpartyInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> Get() => Ok(await _counterpartyManagementService.Get());


        /// <summary>
        ///     Sets counterparty verified read only.
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty to verify.</param>
        /// <param name="request">Verification details.</param>
        /// <returns></returns>
        [HttpPost("{counterpartyId}/verify-read-only")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyVerification)]
        public async Task<IActionResult> VerifyReadOnly(int counterpartyId, [FromBody] CounterpartyReadOnlyVerificationRequest request)
        {
            var (isSuccess, _, error) = await _counterpartyVerificationService.VerifyAsReadOnly(counterpartyId, request.Reason);

            return isSuccess
                ? (IActionResult) NoContent()
                : BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Sets counterparty fully verified.
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty to verify.</param>
        /// <param name="request">Verification details.</param>
        /// <returns></returns>
        [HttpPost("{counterpartyId}/verify-full-access")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyVerification)]
        public async Task<IActionResult> VerifyFullAccess(int counterpartyId, [FromBody] CounterpartyFullAccessVerificationRequest request)
        {
            var (isSuccess, _, error) = await _counterpartyVerificationService.VerifyAsFullyAccessed(counterpartyId, request.ContractKind, request.Reason);

            return isSuccess
                ? (IActionResult) NoContent()
                : BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Sets counterparty declined verification.
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty to verify.</param>
        /// <param name="request">Verification details.</param>
        /// <returns></returns>
        [HttpPost("{counterpartyId}/decline-verification")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyVerification)]
        public async Task<IActionResult> DeclineVerification(int counterpartyId, [FromBody] CounterpartyDeclinedVerificationRequest request)
        {
            var (isSuccess, _, error) = await _counterpartyVerificationService.DeclineVerification(counterpartyId, request.Reason);

            return isSuccess
                ? (IActionResult) NoContent()
                : BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Gets all agencies of a counterparty.
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id.</param>
        /// <returns></returns>
        [HttpGet("{counterpartyId}/agencies")]
        [ProducesResponseType(typeof(List<AgencyInfo>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> GetAgencies(int counterpartyId) => Ok(await _counterpartyManagementService.GetAllCounterpartyAgencies(counterpartyId));


        /// <summary>
        ///     Updates counterparty information.
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty.</param>
        /// <param name="updateCounterpartyRequest">New counterparty information.</param>
        /// <returns></returns>
        [HttpPut("{counterpartyId}")]
        [ProducesResponseType(typeof(CounterpartyInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> UpdateCounterparty(int counterpartyId, [FromBody] CounterpartyEditRequest updateCounterpartyRequest)
        {
            var (_, isFailure, savedCounterpartyInfo, error) =
                await _counterpartyManagementService.Update(updateCounterpartyRequest, counterpartyId);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(savedCounterpartyInfo);
        }


        /// <summary>
        ///  Deactivates specified counterparty
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty.</param>
        /// <param name="request">Request data for deactivation.</param>
        /// <returns></returns>
        [HttpPost("{counterpartyId}/deactivate")]
        [ProducesResponseType(typeof(CounterpartyInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> DeactivateCounterparty(int counterpartyId, ActivityStatusChangeRequest request)
        {
            var (_, isFailure, error) = await _counterpartyManagementService.DeactivateCounterparty(counterpartyId, request.Reason);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///  Activates specified counterparty
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty.</param>
        /// <param name="request">Request data for Activation.</param>
        /// <returns></returns>
        [HttpPost("{counterpartyId}/activate")]
        [ProducesResponseType(typeof(CounterpartyInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> ActivateCounterparty(int counterpartyId, ActivityStatusChangeRequest request)
        {
            var (_, isFailure, error) = await _counterpartyManagementService.ActivateCounterparty(counterpartyId, request.Reason);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///  Returns counterparties predictions when searching
        /// </summary>
        /// <param name="query">The search query text.</param>
        /// <returns></returns>
        [HttpGet("predictions")]
        [ProducesResponseType(typeof(List<CounterpartyPrediction>), (int) HttpStatusCode.OK)]
        [AdministratorPermissions(AdministratorPermissions.PaymentLinkGeneration)]
        public async Task<IActionResult> GetCounterpartyPredictions(string query)
        {
            var result = await _counterpartyManagementService.GetCounterpartiesPredictions(query);
            return Ok(result);
        }


        /// <summary>
        /// Uploads a contract pdf file
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty to save the contract file for</param>
        /// <param name="file">A pdf file of the contract with the given counterparty</param>
        [HttpPut("{counterpartyId}/contract-file")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> AddContractFile(int counterpartyId, [FromForm] IFormFile file)
        {
            var result = await _contractFileManagementService.Add(counterpartyId, file);

            return OkOrBadRequest(result);
        }


        /// <summary>
        /// Downloads a contract pdf file
        /// </summary>
        /// <param name="counterpartyId">Id of the counterparty to load the contract file for</param>
        [HttpGet("{counterpartyId}/contract-file")]
        [ProducesResponseType(typeof(FileStreamResult), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyManagement)]
        public async Task<IActionResult> GetContractFile(int counterpartyId)
        {
            var (_, isFailure, (stream, contentType), error) = await _contractFileManagementService.Get(counterpartyId);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return File(stream, contentType);
        }


        private readonly ICounterpartyManagementService _counterpartyManagementService;
        private readonly IContractFileManagementService _contractFileManagementService;
        private readonly ICounterpartyVerificationService _counterpartyVerificationService;
    }
}