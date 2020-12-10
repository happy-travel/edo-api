using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AgentExistingFilters;
using HappyTravel.Edo.Api.Filters.Authorization.CounterpartyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Filters.OData;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Common.Enums;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Query.Validators;
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
        /// <returns></returns>
        [ProducesResponseType(typeof(List<PaymentHistoryData>), (int) HttpStatusCode.OK)]
        [AgentRequired]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [HttpGet("history/agent")]
        // TODO: uncomment after implementation pagination in fronted
        // [EnablePaginatedQuery(MaxTop = 100)]
        [EnableQuery]
        public async Task<ActionResult<List<PaymentHistoryData>>> GetAgentHistory()
        {
            var agent = await _agentContextService.GetAgent();
            return Ok(_paymentHistoryService.GetAgentHistory(agent));
        }


        /// <summary>
        ///     Gets payment history for an agency.
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(List<PaymentHistoryData>), (int) HttpStatusCode.OK)]
        [HttpGet("history/agency")]
        [MinCounterpartyState(CounterpartyStates.FullAccess)]
        [InAgencyPermissions(InAgencyPermissions.ObservePaymentHistory)]
        // TODO: uncomment after implementation pagination in fronted
        // [EnablePaginatedQuery(MaxTop = 100)]
        [EnableQuery]
        public async Task<ActionResult<IQueryable<PaymentHistoryData>>> GetAgencyHistory()
        {
            var agent = await _agentContextService.GetAgent();
            return Ok (_paymentHistoryService.GetAgencyHistory(agent));
        }


        private readonly IPaymentHistoryService _paymentHistoryService;
        private readonly IAgentContextService _agentContextService;
    }
}