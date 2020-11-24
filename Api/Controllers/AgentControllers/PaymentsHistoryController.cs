using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/payments")]
    [Produces("application/json")]
    public class PaymentsHistoryController : ControllerBase
    {
        public PaymentsHistoryController(IPaymentHistoryService paymentHistoryService, IAgentContextService agentContextService)
        {
            _paymentHistoryService = paymentHistoryService;
            _agentContextService = agentContextService;
        }


        /// <summary>
        ///     Gets payment history for current agent.
        /// </summary>
        /// <param name="agencyId">The agent could have relations with different agencies</param>
        /// <param name="historyRequest"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(List<PaymentHistoryData>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [AgentRequired]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [HttpPost("history/agencies/{agencyId}/agent")]
        public async Task<IActionResult> GetAgentHistory([Required] int agencyId, [FromBody] PaymentHistoryRequest historyRequest)
        {
            // TODO: Remove agencyId from route NIJO-1075
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, response, error) = await _paymentHistoryService.GetAgentHistory(historyRequest, agent);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        /// <summary>
        ///     Gets payment history for an agency.
        /// </summary>
        /// <param name="agencyId"></param>
        /// <param name="historyRequest"></param>
        /// <returns></returns>
        [ProducesResponseType(typeof(List<PaymentHistoryData>), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [HttpPost("history/agencies/{agencyId}")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.ObservePaymentHistory)]
        public async Task<IActionResult> GetAgencyHistory([Required] int agencyId, [FromBody] PaymentHistoryRequest historyRequest)
        {
            // TODO: Remove agencyId from route NIJO-1075
            var agent = await _agentContextService.GetAgent();
            var (_, isFailure, response, error) = await _paymentHistoryService.GetAgencyHistory(historyRequest, agent);
            if (isFailure)
                return BadRequest(error);

            return Ok(response);
        }


        private readonly IPaymentHistoryService _paymentHistoryService;
        private readonly IAgentContextService _agentContextService;
    }
}