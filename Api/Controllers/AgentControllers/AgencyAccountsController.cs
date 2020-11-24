using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.InAgencyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Models;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AgentControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/agency-accounts")]
    [Produces("application/json")]
    public class AgencyAccountsController : BaseController
    {
        public AgencyAccountsController(IAccountPaymentService accountPaymentService,
            IAgentContextService agentContextService)
        {
            _accountPaymentService = accountPaymentService;
            _agentContextService = agentContextService;
        }
        
        
        /// <summary>
        ///     Transfers money from an agency account to a child agency account
        /// </summary>
        /// <param name="payerAccountId">Id of the payer agency account</param>
        /// <param name="recipientAccountId">Id of the recepient agency account</param>
        /// <param name="amount">Amount of money to transfer</param>
        [HttpPost("{payerAccountId}/transfer/{recipientAccountId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [InAgencyPermissions(InAgencyPermissions.AgencyToChildTransfer)]
        public async Task<IActionResult> TransferToChildAgency(int payerAccountId, int recipientAccountId, [FromBody] MoneyAmount amount)
        {
            // TODO: Remove payerAccountId from route NIJO-1075
            var (isSuccess, _, error) = await _accountPaymentService.TransferToChildAgency(recipientAccountId, amount, await _agentContextService.GetAgent());

            return isSuccess
                ? NoContent()
                : (IActionResult)BadRequest(ProblemDetailsBuilder.Build(error));
        }
        
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly IAgentContextService _agentContextService;
    }
}