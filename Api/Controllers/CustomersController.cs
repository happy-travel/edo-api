using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Services.Customers;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}/customers")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        public CustomersController(IRegistrationService registrationService, ICustomerContext customerContext)
        {
            _registrationService = registrationService;
            _customerContext = customerContext;
        }

        /// <summary>
        ///     Registers master customer with related company
        /// </summary>
        /// <param name="request">Master customer registration request.</param>
        /// <returns></returns>
        [HttpPost("master/register")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterMasterCustomer([FromBody] RegisterMasterCustomerRequest request)
        {
            var externalIdentity = HttpContext.User.Claims.SingleOrDefault(c=> c.Type == "sub")?.Value;
            if(string.IsNullOrWhiteSpace(externalIdentity))
                return BadRequest(ProblemDetailsBuilder.Build("User sub claim is required"));
                
            var registerResult = await _registrationService.RegisterMasterCustomer(request.Company, request.MasterCustomer, externalIdentity);
            if (registerResult.IsFailure)
                return BadRequest(ProblemDetailsBuilder.Build(registerResult.Error));

            return Ok();
        }
        
        /// <summary>
        ///     Get current customer.
        /// </summary>
        /// <returns>Current customer information.</returns>
        [HttpGet]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCurrentCustomer()
        {
            var (_, isFailure, customer, error) = await _customerContext.GetCustomer();
            if(isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));
            
            return Ok(new CustomerInfo(customer.Email,
                customer.LastName, 
                customer.FirstName,
                customer.Title,
                customer.Position));
        }
        
        private readonly IRegistrationService _registrationService;
        private readonly ICustomerContext _customerContext;
    }
}