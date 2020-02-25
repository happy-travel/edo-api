using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Filters.Authorization.CompanyStatesFilters;
using HappyTravel.Edo.Api.Filters.Authorization.InCompanyPermissionFilters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/{v:apiVersion}")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        public CustomersController(ICustomerRegistrationService customerRegistrationService, ICustomerContext customerContext,
            ICustomerInvitationService customerInvitationService,
            ITokenInfoAccessor tokenInfoAccessor,
            ICustomerSettingsManager customerSettingsManager,
            ICustomerPermissionManagementService permissionManagementService,
            IHttpClientFactory httpClientFactory)
        {
            _customerRegistrationService = customerRegistrationService;
            _customerContext = customerContext;
            _customerInvitationService = customerInvitationService;
            _tokenInfoAccessor = tokenInfoAccessor;
            _customerSettingsManager = customerSettingsManager;
            _permissionManagementService = permissionManagementService;
            _httpClientFactory = httpClientFactory;
        }


        /// <summary>
        ///     Registers master customer with related company
        /// </summary>
        /// <param name="request">Master customer registration request.</param>
        /// <returns></returns>
        [HttpPost("customers/register/master")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterCustomerWithCompany([FromBody] RegisterCustomerWithCompanyRequest request)
        {
            var externalIdentity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(externalIdentity))
                return BadRequest(ProblemDetailsBuilder.Build("User sub claim is required"));

            var email = await GetUserEmail();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(ProblemDetailsBuilder.Build("E-mail claim is required"));

            var registerResult = await _customerRegistrationService.RegisterWithCompany(request.Customer, request.Company,
                externalIdentity, email);

            if (registerResult.IsFailure)
                return BadRequest(ProblemDetailsBuilder.Build(registerResult.Error));

            return NoContent();
        }


        /// <summary>
        ///     Registers regular customer.
        /// </summary>
        /// <param name="request">Regular customer registration request.</param>
        /// <returns></returns>
        [HttpPost("customers/register")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RegisterInvitedCustomer([FromBody] RegisterInvitedCustomerRequest request)
        {
            var identity = _tokenInfoAccessor.GetIdentity();
            if (string.IsNullOrWhiteSpace(identity))
                return BadRequest(ProblemDetailsBuilder.Build("User sub claim is required"));

            var email = await GetUserEmail();
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(ProblemDetailsBuilder.Build("E-mail claim is required"));

            var (_, isFailure, error) = await _customerRegistrationService
                .RegisterInvited(request.RegistrationInfo, request.InvitationCode, identity, email);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Invite regular customer.
        /// </summary>
        /// <param name="request">Regular customer registration request.</param>
        /// <returns></returns>
        [HttpPost("customers/invitations")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [InCompanyPermissions(InCompanyPermissions.CustomerInvitation)]
        public async Task<IActionResult> InviteCustomer([FromBody] CustomerInvitationInfo request)
        {
            var (_, isFailure, error) = await _customerInvitationService.SendInvitation(request);
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return NoContent();
        }


        /// <summary>
        ///     Get invitation data.
        /// </summary>
        /// <param name="code">Invitation code.</param>
        /// <returns>Invitation data, including pre-filled registration information.</returns>
        [HttpGet("customers/invitations/{code}")]
        [ProducesResponseType(typeof(CustomerInvitationInfo), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetInvitationData(string code)
        {
            var (_, isFailure, invitationInfo, error) = await _customerInvitationService
                .GetPendingInvitation(code);

            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(invitationInfo);
        }


        /// <summary>
        ///     Get current customer.
        /// </summary>
        /// <returns>Current customer information.</returns>
        [HttpGet("customers")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetCurrentCustomer()
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(error));

            return Ok(new CustomerDescription(customerInfo.Email,
                customerInfo.LastName,
                customerInfo.FirstName,
                customerInfo.Title,
                customerInfo.Position,
                await _customerContext.GetCustomerCompanies()));
        }


        /// <summary>
        ///     Sets user frontend application settings.
        /// </summary>
        /// <param name="settings">Settings in dynamic JSON-format</param>
        /// <returns></returns>
        [RequestSizeLimit(256 * 1024)]
        [HttpPut("customers/settings/application")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SetApplicationSettings([FromBody] JToken settings)
        {
            var (_, isFailure, customerInfo, customerInfoError) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(customerInfoError));

            var (isSuccess, _, error) = await _customerSettingsManager.SetAppSettings(customerInfo, settings.ToString(Formatting.None));
            return isSuccess
                ? (IActionResult) NoContent()
                : BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Gets user frontend application settings.
        /// </summary>
        /// <returns>Settings in dynamic JSON-format</returns>
        [HttpGet("customers/settings/application")]
        [ProducesResponseType(typeof(JToken), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetApplicationSettings()
        {
            var (_, isFailure, customerInfo, customerInfoError) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(customerInfoError));

            var (isSuccess, _, settings, getSettingsError) = await _customerSettingsManager.GetAppSettings(customerInfo);
            return isSuccess
                ? (IActionResult) Ok(JToken.Parse(settings))
                : BadRequest(ProblemDetailsBuilder.Build(getSettingsError));
        }


        /// <summary>
        ///     Sets user preferences.
        /// </summary>
        /// <param name="settings">Settings in JSON-format</param>
        /// <returns></returns>
        [HttpPut("customers/settings/user")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SetUserSettings([FromBody] CustomerUserSettings settings)
        {
            var (_, isFailure, customerInfo, customerInfoError) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(customerInfoError));

            var (isSuccess, _, error) = await _customerSettingsManager.SetUserSettings(customerInfo, settings);
            return isSuccess
                ? (IActionResult) NoContent()
                : BadRequest(ProblemDetailsBuilder.Build(error));
        }


        /// <summary>
        ///     Gets user preferences.
        /// </summary>
        /// <returns>Settings in JSON-format</returns>
        [HttpGet("customers/settings/user")]
        [ProducesResponseType(typeof(CustomerUserSettings), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetUserSettings()
        {
            var (_, isFailure, customerInfo, customerInfoError) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return BadRequest(ProblemDetailsBuilder.Build(customerInfoError));

            var (isSuccess, _, settings, getSettingsError) = await _customerSettingsManager.GetUserSettings(customerInfo);
            return isSuccess
                ? (IActionResult) Ok(settings)
                : BadRequest(ProblemDetailsBuilder.Build(getSettingsError));
        }


        /// <summary>
        ///     Assigns permissions for user in current company.
        /// </summary>
        /// <param name="companyId">ID of the user's company to apply user permissions.</param>
        /// <param name="branchId">ID of the company's branch to apply user permissions.</param>
        /// <param name="customerId">ID of the customer.</param>
        /// <param name="request">Verification reason.</param>
        /// <returns></returns>
        [HttpPut("companies/{companyId}/{branchId}/customers/{customerId}/permissions")]
        [ProducesResponseType((int) HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), (int) HttpStatusCode.BadRequest)]
        [MinCompanyState(CompanyStates.FullAccess)]
        [InCompanyPermissions(InCompanyPermissions.PermissionManagement)]
        public async Task<IActionResult> AssignPermissions(int companyId, int branchId, int customerId, [FromBody] AssignInCompanyPermissionsRequest request)
        {
            var (isSuccess, _, error) = await _permissionManagementService.SetInCompanyPermissions(companyId, branchId, customerId, request.Permissions);

            return isSuccess
                ? (IActionResult) NoContent()
                : BadRequest(ProblemDetailsBuilder.Build(error));
        }


        private async Task<string> GetUserEmail()
        {
            // TODO: Move this logic to separate service
            using var discoveryClient = _httpClientFactory.CreateClient(HttpClientNames.OpenApiDiscovery);
            using var userInfoClient = _httpClientFactory.CreateClient(HttpClientNames.OpenApiUserInfo);

            var doc = await discoveryClient.GetDiscoveryDocumentAsync();
            var token = await _tokenInfoAccessor.GetAccessToken();

            return (await userInfoClient.GetUserInfoAsync(new UserInfoRequest {Token = token, Address = doc.UserInfoEndpoint }))
                .Claims
                .SingleOrDefault(c => c.Type == "email")
                ?.Value;
        }


        private readonly ICustomerContext _customerContext;
        private readonly ICustomerInvitationService _customerInvitationService;
        private readonly ICustomerRegistrationService _customerRegistrationService;
        private readonly ICustomerSettingsManager _customerSettingsManager;
        private readonly ICustomerPermissionManagementService _permissionManagementService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenInfoAccessor _tokenInfoAccessor;
    }
}