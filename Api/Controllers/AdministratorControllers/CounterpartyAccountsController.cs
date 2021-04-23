using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Filters.Authorization.AdministratorFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Counterparties;
using HappyTravel.Edo.Api.Models.Management.Enums;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers.AdministratorControllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/admin")]
    [Produces("application/json")]
    public class CounterpartyAccountsController : BaseController
    {
        public CounterpartyAccountsController(IAccountPaymentService accountPaymentService)
        {
            _accountPaymentService = accountPaymentService;
        }


        /// <summary>
        ///     Gets counterparty accounts list
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id</param>
        [HttpGet("{counterpartyId}/counterparty-accounts")]
        [ProducesResponseType(typeof(List<FullCounterpartyAccountInfo>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceObservation)]
        public async Task<IActionResult> GetCounterpartyAccounts([FromRoute] int counterpartyId)
            => Ok(await _accountPaymentService.GetCounterpartyAccounts(counterpartyId));


        /// <summary>
        /// Changes a counterparty account activity state
        /// </summary>
        /// <param name="counterpartyId">Counterparty Id</param>
        /// <param name="counterpartyAccountId">Counterparty account Id</param>
        /// <param name="counterpartyAccountRequest">Editable counterparty account settings</param>
        [HttpPut("{counterpartyId}/counterparty-accounts/{counterpartyAccountId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
        [AdministratorPermissions(AdministratorPermissions.CounterpartyBalanceReplenishAndSubtract)]
        public async Task<IActionResult> SetCounterpartyAccountSettings([FromRoute] int counterpartyId, [FromRoute] int counterpartyAccountId, [FromBody] СounterpartyAccountRequest counterpartyAccountRequest)
        {
            var (_, isFailure, error) = await _accountPaymentService.SetСounterpartyAccountSettings(new СounterpartyAccountSettings(counterpartyId, counterpartyAccountId, counterpartyAccountRequest.IsActive));
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok();
        }


        private readonly IAccountPaymentService _accountPaymentService;
    }
}
