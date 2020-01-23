using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace HappyTravel.Edo.Api.Services.Payments.Payfort
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
                    merchantReference: model.MerchantReference));


                CreditCardPaymentStatuses GetStatus(PayfortPaymentResponse payment)
                {
                    switch (payment.ResponseCode)
                    {
                        case PayfortConstants.PaymentSuccessResponseCode:
                        case PayfortConstants.AuthorizationSuccessResponseCode: return CreditCardPaymentStatuses.Success;
                        case PayfortConstants.PaymentSecure3dResponseCode: return CreditCardPaymentStatuses.Secure3d;
                        default: return CreditCardPaymentStatuses.Failed;
                    }
                }
            }
        }


        public async Task<Result<CreditCardCaptureResult>> Capture(CreditCardCaptureMoneyRequest moneyRequest)
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
                return Result.Fail<CreditCardCaptureResult>(ex.Message);
            }


            HttpContent GetSignedContent()
            {
                var paymentRequest = new PayfortCaptureRequest(
                    signature: string.Empty,
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: moneyRequest.MerchantReference,
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


            Result<CreditCardCaptureResult> CreateResult(PayfortCaptureResponse model)
            {
                return IsSuccess(model)
                    ? Result.Ok(new CreditCardCaptureResult(
                        model.FortId, 
                        $"{model.ResponseCode}: {model.ResponseMessage}", 
                        model.MerchantReference))
                    : Result.Fail<CreditCardCaptureResult>($"Unable capture payment for the booking '{moneyRequest.MerchantReference}': '{model.ResponseMessage}'");

                bool IsSuccess(PayfortCaptureResponse captureResponse) => captureResponse.ResponseCode == PayfortConstants.CaptureSuccessResponseCode;
            }
        }


        public async Task<Result<CreditCardVoidResult>> Void(CreditCardVoidMoneyRequest moneyRequest)
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
                return Result.Fail<CreditCardVoidResult>(ex.Message);
            }


            HttpContent GetSignedContent()
            {
                var paymentRequest = new PayfortVoidRequest(
                    signature: string.Empty,
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: moneyRequest.MerchantReference,
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


            Result<CreditCardVoidResult> CreateResult(PayfortVoidResponse model)
            {
                return IsSuccess(model)
                    ? Result.Ok(new CreditCardVoidResult(
                        model.FortId, 
                        $"{model.ResponseCode}: {model.ResponseMessage}", 
                        model.MerchantReference))
                    : Result.Fail<CreditCardVoidResult>($"Unable void payment for the booking '{moneyRequest.MerchantReference}': '{model.ResponseMessage}'");

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
                    merchantReference: request.MerchantReference,
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


        private static Result<decimal> FromPayfortAmount(string amountString, string currencyString)
        {
            if (!Enum.TryParse<Currencies>(currencyString, out var currency))
                return Result.Fail<decimal>($"Invalid currency in response: {currencyString}");

            if (!decimal.TryParse(amountString, out var amount))
                return Result.Fail<decimal>("");

            var result = amount / PayfortConstants.ExponentMultipliers[currency];
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