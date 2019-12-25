using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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


        public Task<Result<CreditCardPaymentResult>> Authorize(CreditCardPaymentRequest request) => MakePayment(request, PaymentCommandType.Authorization);


        public Task<Result<CreditCardPaymentResult>> Pay(CreditCardPaymentRequest request) => MakePayment(request, PaymentCommandType.Purchase);


        public Result<CreditCardPaymentResult> ParsePaymentResponse(JObject response)
        {
            return ParseResponse<PayfortPaymentResponse>(response)
                .OnSuccess(CheckResponseSignature)
                .OnSuccess(CreateResult);

            Result<PayfortPaymentResponse> CheckResponseSignature(PayfortPaymentResponse model) => CheckSignature(response, model);


            Result<CreditCardPaymentResult> CreateResult(PayfortPaymentResponse model)
            {
                var (_, isFailure, amount, error) = FromPayfortAmount(model.Amount, model.Currency);
                if (isFailure)
                    return Result.Fail<CreditCardPaymentResult>(error);

                return Result.Ok(new CreditCardPaymentResult(
                    referenceCode: model.SettlementReference,
                    secure3d: model.Secure3d,
                    authorizationCode: model.AuthorizationCode,
                    externalCode: model.FortId,
                    expirationDate: model.ExpirationDate,
                    cardNumber: model.CardNumber,
                    status: GetStatus(model),
                    message: $"{model.ResponseCode}: {model.ResponseMessage}",
                    amount: amount,
                    internalReferenceCode: model.MerchantReference));


                PaymentStatuses GetStatus(PayfortPaymentResponse payment)
                {
                    switch (payment.ResponseCode)
                    {
                        case PayfortConstants.PaymentSuccessResponseCode:
                        case PayfortConstants.AuthorizationSuccessResponseCode: return PaymentStatuses.Success;
                        case PayfortConstants.PaymentSecure3dResponseCode: return PaymentStatuses.Secure3d;
                        default: return PaymentStatuses.Failed;
                    }
                }
            }
        }


        public async Task<Result> Capture(CreditCardCaptureMoneyRequest moneyRequest)
        {
            try
            {
                var requestContent = GetSignedContent();
                var client = _clientFactory.CreateClient(HttpClientNames.Payfort);
                using (var response = await client.PostAsync(_options.PaymentUrl, requestContent))
                {
                    return await GetContent(response)
                        .OnSuccess(Parse)
                        .OnSuccess(CheckResponseSignature)
                        .OnSuccess(CreateResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Fail(ex.Message);
            }


            HttpContent GetSignedContent()
            {
                var paymentRequest = new PayfortCaptureRequest(
                    signature: string.Empty,
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: moneyRequest.InternalReferenceCode,
                    amount: ToPayfortAmount(moneyRequest.Amount, moneyRequest.Currency),
                    currency: moneyRequest.Currency.ToString(),
                    language: moneyRequest.LanguageCode,
                    fortId: moneyRequest.ExternalId
                );

                var jObject = JObject.FromObject(paymentRequest, Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortCaptureRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, SerializerSettings);

                return new StringContent(json, Encoding.UTF8, "application/json");
            }


            Result<(PayfortCaptureResponse model, JObject response)> Parse(string content)
                => GetJObject(content)
                    .OnSuccess(response => ParseResponse<PayfortCaptureResponse>(response)
                        .Map(model => (model, response))
                    );


            Result<PayfortCaptureResponse> CheckResponseSignature((PayfortCaptureResponse model, JObject response) data)
                => CheckSignature(data.response, data.model);


            Result CreateResult(PayfortCaptureResponse model)
            {
                return IsSuccess(model)
                    ? Result.Ok()
                    : Result.Fail($"Unable capture payment for booking '{moneyRequest.InternalReferenceCode}': '{model.ResponseMessage}'");

                bool IsSuccess(PayfortCaptureResponse captureResponse) => captureResponse.ResponseCode == PayfortConstants.CaptureSuccessResponseCode;
            }
        }


        public async Task<Result> Void(CreditCardVoidMoneyRequest moneyRequest)
        {
            try
            {
                var requestContent = GetSignedContent();
                var client = _clientFactory.CreateClient(HttpClientNames.Payfort);
                using (var response = await client.PostAsync(_options.PaymentUrl, requestContent))
                {
                    return await GetContent(response)
                        .OnSuccess(Parse)
                        .OnSuccess(CheckResponseSignature)
                        .OnSuccess(CreateResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Fail(ex.Message);
            }


            HttpContent GetSignedContent()
            {
                var paymentRequest = new PayfortVoidRequest(
                    signature: string.Empty,
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: moneyRequest.InternalReferenceCode,
                    language: moneyRequest.LanguageCode,
                    fortId: moneyRequest.ExternalId
                );

                var jObject = JObject.FromObject(paymentRequest, Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortVoidRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, SerializerSettings);

                return new StringContent(json, Encoding.UTF8, "application/json");
            }


            Result<(PayfortVoidResponse model, JObject response)> Parse(string content)
                => GetJObject(content)
                    .OnSuccess(response => ParseResponse<PayfortVoidResponse>(response)
                        .Map(model => (model, response))
                    );


            Result<PayfortVoidResponse> CheckResponseSignature((PayfortVoidResponse model, JObject response) data) => CheckSignature(data.response, data.model);


            Result CreateResult(PayfortVoidResponse model)
            {
                return IsSuccess(model)
                    ? Result.Ok()
                    : Result.Fail($"Unable void payment for booking '{moneyRequest.InternalReferenceCode}': '{model.ResponseMessage}'");

                bool IsSuccess(PayfortVoidResponse captureResponse) => captureResponse.ResponseCode == PayfortConstants.VoidSuccessResponseCode;
            }
        }


        private async Task<Result<CreditCardPaymentResult>> MakePayment(CreditCardPaymentRequest request, PaymentCommandType commandType)
        {
            try
            {
                var requestContent = GetSignedContent();
                var client = _clientFactory.CreateClient(HttpClientNames.Payfort);
                using (var response = await client.PostAsync(_options.PaymentUrl, requestContent))
                {
                    return await GetContent(response)
                        .OnSuccess(GetJObject)
                        .OnSuccess(ParsePaymentResponse);
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
                    signature: string.Empty,
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: request.InternalReferenceCode,
                    amount: ToPayfortAmount(request.Amount, request.Currency),
                    currency: request.Currency.ToString(),
                    customerName: request.CustomerName,
                    customerEmail: request.CustomerEmail,
                    customerIp: request.CustomerIp,
                    language: request.LanguageCode,
                    returnUrl: _options.ReturnUrl,
                    settlementReference: request.ReferenceCode,
                    tokenName: request.Token.Code,
                    rememberMe: ToPayfortBoolean(request.Token.Type == PaymentTokenTypes.Stored),
                    cardSecurityCode: GetSecurityCode(),
                    command: GetCommand()
                );

                var jObject = JObject.FromObject(paymentRequest, Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortPaymentRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, SerializerSettings);
                return new StringContent(json, Encoding.UTF8, "application/json");


                // Is not needed for new card.
                string GetSecurityCode() => request.IsNewCard ? null : request.SecurityCode;

                string GetCommand() => commandType == PaymentCommandType.Purchase ? "PURCHASE" : "AUTHORIZATION";
            }
        }


        private Result<JObject> GetJObject(string content)
        {
            try
            {
                return Result.Ok(JObject.Parse(content));
            }
            catch (Exception ex)
            {
                _logger.LogPayfortError($"Error deserializing payfort response: '{content}'");
                return Result.Fail<JObject>($"{ex.Message} for '{content}'");
            }
        }


        private async Task<Result<string>> GetContent(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode
                ? Result.Ok(content)
                : Result.Fail<string>(content);
        }


        private Result<T> ParseResponse<T>(JObject response)
        {
            var model = response.ToObject<T>(Serializer);
            return model == null
                ? Result.Fail<T>($"Invalid payfort payment response: '{response}'")
                : Result.Ok(model);
        }


        private Result<T> CheckSignature<T>(JObject response, T model) where T : ISignedResponse
        {
            var (_, _, signature, _) = _signatureService.Calculate(response, SignatureTypes.Response);
            if (signature != model.Signature)
            {
                _logger.LogPayfortError($"Payfort Payment error: Invalid response signature. Content: '{response}'");
                return Result.Fail<T>("Payfort process payment error");
            }

            return Result.Ok(model);
        }


        private static string ToPayfortBoolean(bool value) => value ? "YES" : "NO";


        private static string ToPayfortAmount(decimal amount, Currencies currency)
            => decimal.ToInt64(amount * PayfortConstants.ExponentMultipliers[currency]).ToString();


        private static Result<decimal> FromPayfortAmount(string amount, string currency)
        {
            if (!Enum.TryParse<Currencies>(currency, out var currencyEnum))
                return Result.Fail<decimal>($"Invalid currency in response: {currency}");

            if (!decimal.TryParse(amount, out var amountNumber))
                return Result.Fail<decimal>("");

            var result = amountNumber / PayfortConstants.ExponentMultipliers[currencyEnum];
            return Result.Ok(result);
        }


        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);
        private readonly IHttpClientFactory _clientFactory;

        private readonly ILogger<PayfortService> _logger;
        private readonly PayfortOptions _options;
        private readonly IPayfortSignatureService _signatureService;
    }
}