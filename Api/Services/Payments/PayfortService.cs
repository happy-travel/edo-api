using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PayfortService : IPayfortService
    {
        public PayfortService(ILogger<PayfortService> logger, IHttpClientFactory clientFactory, IOptions<PayfortOptions> options,
            IPayfortSignatureService signatureService)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _options = options.Value;
            _signatureService = signatureService;
        }

        public async Task<Result<CreditCardPaymentResult>> Pay(CreditCardPaymentRequest request)
        {
            try
            {
                var requestContent = GetSignedContent();
                using (var client = _clientFactory.CreateClient(HttpClientNames.Payfort))
                    using (var response = await client.PostAsync(_options.PaymentUrl, requestContent))
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                            return Result.Fail<CreditCardPaymentResult>(content);
                        var responseObject = JObject.Parse(content);
                        return ProcessPaymentResponse(responseObject);
                    }
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Fail<CreditCardPaymentResult>(ex.Message);
            }

            HttpContent GetSignedContent()
            {
                var paymentRequest = new PayfortPaymentRequest(
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: request.ReferenceCode,
                    amount: ToPayfortAmount(request.Amount, request.Currency),
                    currency: request.Currency.ToString(),
                    customerName: request.CustomerName,
                    customerEmail: request.CustomerEmail,
                    customerIp: request.CustomerIp,
                    language: request.LanguageCode,
                    returnUrl: _options.ReturnUrl,
                    settlementReference: request.ReferenceCode,
                    tokenName: request.Token,
                    rememberMe: ToPayfortBoolean(request.IsStoredToken),
                    cardSecurityCode: GetSecurityCode(),
                    signature: string.Empty
                );

                var jObject = JObject.FromObject(paymentRequest, Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortPaymentRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, SerializerSettings);
                return new StringContent(json, Encoding.UTF8, "application/json");

                // Does not need for new card.
                string GetSecurityCode() => request.IsNewCard ? null : request.SecurityCode;
            }
        }

        public Result<CreditCardPaymentResult> ProcessPaymentResponse(JObject response)
        {
            var model = response.ToObject<PayfortPaymentResponse>(Serializer);

            if (model == null)
                return Result.Fail<CreditCardPaymentResult>($"Invalid payfort payment response: {response}");

            var (_, _, signature, _) = _signatureService.Calculate(response, SignatureTypes.Response);
            if (signature != model.Signature)
            {
                _logger.LogPayfortError($"Payfort Payment error: Invalid response signature. content: {response}");
                return Result.Fail<CreditCardPaymentResult>($"Payfort process payment error");
            }

            var status = GetStatus(model);

            return Result.Ok(new CreditCardPaymentResult(model, status));

            PaymentStatuses GetStatus(PayfortPaymentResponse payment)
            {
                switch (payment.ResponseCode)
                {
                    case PayfortConstants.PaymentSuccessResponseCode: return PaymentStatuses.Success;
                    case PayfortConstants.PaymentSecure3dResponseCode: return PaymentStatuses.Secure3d;
                    default: return PaymentStatuses.Failed;
                }
            }
        }

        private static string ToPayfortBoolean(bool value) =>
            value ? "YES" : "NO";

        private static string ToPayfortAmount(decimal amount, Currencies currency) =>
            decimal.ToInt64(amount * PayfortConstants.ExponentMultipliers[currency]).ToString();

        private readonly ILogger<PayfortService> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private readonly PayfortOptions _options;
        private readonly IPayfortSignatureService _signatureService;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);
    }
}
