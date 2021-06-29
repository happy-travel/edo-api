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
using HappyTravel.Money.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.Payfort
{
    public class PayfortService : IPayfortService
    {
        public PayfortService(ILogger<PayfortService> logger, IHttpClientFactory clientFactory, IOptions<PayfortOptions> options,
            IPayfortSignatureService signatureService, IPayfortResponseParser payfortResponseParser)
        {
            _logger = logger;
            _clientFactory = clientFactory;
            _options = options.Value;
            _signatureService = signatureService;
            _payfortResponseParser = payfortResponseParser;
        }


        public Task<Result<CreditCardPaymentResult>> Authorize(CreditCardPaymentRequest request) => MakePayment(request, PaymentCommandType.Authorization);


        public Task<Result<CreditCardPaymentResult>> Pay(CreditCardPaymentRequest request) => MakePayment(request, PaymentCommandType.Purchase);


        public async Task<Result<CreditCardCaptureResult>> Capture(CreditCardCaptureMoneyRequest moneyRequest)
        {
            try
            {
                var requestContent = GetSignedContent();
                var client = _clientFactory.CreateClient(HttpClientNames.Payfort);
                using (var response = await client.PostAsync(_options.PaymentUrl, requestContent))
                {
                    return await GetContent(response)
                        .Bind(Parse)
                        .Bind(CheckResponseSignature)
                        .Bind(CreateResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Failure<CreditCardCaptureResult>(ex.Message);
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

                var jObject = JObject.FromObject(paymentRequest, PayfortSerializationSettings.Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortCaptureRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, PayfortSerializationSettings.SerializerSettings);

                return new StringContent(json, Encoding.UTF8, "application/json");
            }


            Result<(PayfortCaptureResponse model, JObject response)> Parse(string content)
                => GetJObject(content)
                    .Bind(response => _payfortResponseParser.Parse<PayfortCaptureResponse>(response)
                        .Map(model => (model, response))
                    );


            Result<PayfortCaptureResponse> CheckResponseSignature((PayfortCaptureResponse model, JObject response) data)
                => _signatureService.Check(data.response, data.model);


            Result<CreditCardCaptureResult> CreateResult(PayfortCaptureResponse model)
            {
                return IsSuccess(model)
                    ? Result.Success(new CreditCardCaptureResult(
                        model.FortId, 
                        $"{model.ResponseCode}: {model.ResponseMessage}", 
                        model.MerchantReference))
                    : Result.Failure<CreditCardCaptureResult>($"Unable capture payment for the booking '{moneyRequest.MerchantReference}': '{model.ResponseMessage}'");

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
                        .Bind(Parse)
                        .Bind(CheckResponseSignature)
                        .Bind(CreateResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Failure<CreditCardVoidResult>(ex.Message);
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

                var jObject = JObject.FromObject(paymentRequest, PayfortSerializationSettings.Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortVoidRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, PayfortSerializationSettings.SerializerSettings);

                return new StringContent(json, Encoding.UTF8, "application/json");
            }


            Result<(PayfortVoidResponse model, JObject response)> Parse(string content)
                => GetJObject(content)
                    .Bind(response => _payfortResponseParser.Parse<PayfortVoidResponse>(response)
                        .Map(model => (model, response))
                    );


            Result<PayfortVoidResponse> CheckResponseSignature((PayfortVoidResponse model, JObject response) data) => _signatureService.Check(data.response, data.model);


            Result<CreditCardVoidResult> CreateResult(PayfortVoidResponse model)
            {
                return IsSuccess(model)
                    ? Result.Success(new CreditCardVoidResult(
                        model.FortId, 
                        $"{model.ResponseCode}: {model.ResponseMessage}", 
                        model.MerchantReference))
                    : Result.Failure<CreditCardVoidResult>($"Unable void payment for the booking '{moneyRequest.MerchantReference}': '{model.ResponseMessage}'");

                bool IsSuccess(PayfortVoidResponse captureResponse) => captureResponse.ResponseCode == PayfortConstants.VoidSuccessResponseCode;
            }
        }


        public async Task<Result<CreditCardRefundResult>> Refund(CreditCardRefundMoneyRequest moneyRequest)
        {
            try
            {
                var requestContent = GetSignedContent();
                var client = _clientFactory.CreateClient(HttpClientNames.Payfort);
                using var response = await client.PostAsync(_options.PaymentUrl, requestContent);

                return await GetContent(response)
                    .Bind(Parse)
                    .Bind(CheckResponseSignature)
                    .Bind(CreateResult);
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Failure<CreditCardRefundResult>(ex.Message);
            }


            HttpContent GetSignedContent()
            {
                var paymentRequest = new PayfortRefundRequest(
                    signature: string.Empty,
                    accessCode: _options.AccessCode,
                    merchantIdentifier: _options.Identifier,
                    merchantReference: moneyRequest.MerchantReference,
                    amount: ToPayfortAmount(moneyRequest.Amount, moneyRequest.Currency),
                    currency: moneyRequest.Currency.ToString(),
                    language: moneyRequest.LanguageCode,
                    fortId: moneyRequest.ExternalId
                );

                var jObject = JObject.FromObject(paymentRequest, PayfortSerializationSettings.Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortRefundRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, PayfortSerializationSettings.SerializerSettings);

                return new StringContent(json, Encoding.UTF8, "application/json");
            }


            Result<(PayfortRefundResponse model, JObject response)> Parse(string content)
                => GetJObject(content)
                    .Bind(response => _payfortResponseParser.Parse<PayfortRefundResponse>(response)
                        .Map(model => (model, response))
                    );


            Result<PayfortRefundResponse> CheckResponseSignature((PayfortRefundResponse model, JObject response) data)
                => _signatureService.Check(data.response, data.model);


            Result<CreditCardRefundResult> CreateResult(PayfortRefundResponse model)
            {
                return IsSuccess(model)
                    ? Result.Success(new CreditCardRefundResult(
                        model.FortId,
                        $"{model.ResponseCode}: {model.ResponseMessage}",
                        model.MerchantReference))
                    : Result.Failure<CreditCardRefundResult>($"Unable refund payment for the booking '{moneyRequest.MerchantReference}': '{model.ResponseMessage}'");

                bool IsSuccess(PayfortRefundResponse refundResponse) => refundResponse.ResponseCode == PayfortConstants.RefundSuccessResponseCode;
            }
        }


        private async Task<Result<CreditCardPaymentResult>> MakePayment(CreditCardPaymentRequest request, PaymentCommandType commandType)
        {
            try
            {
                var requestContent = GetSignedContent();
                var client = _clientFactory.CreateClient(HttpClientNames.Payfort);
                using var response = await client.PostAsync(_options.PaymentUrl, requestContent);
                
                return await GetContent(response)
                    .Bind(GetJObject)
                    .Bind(_payfortResponseParser.ParsePaymentResponse);
            }
            catch (Exception ex)
            {
                _logger.LogPayfortClientException(ex);
                return Result.Failure<CreditCardPaymentResult>("Payment error");
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

                var jObject = JObject.FromObject(paymentRequest, PayfortSerializationSettings.Serializer);
                var (_, _, signature, _) = _signatureService.Calculate(jObject, SignatureTypes.Request);
                paymentRequest = new PayfortPaymentRequest(paymentRequest, signature);
                var json = JsonConvert.SerializeObject(paymentRequest, PayfortSerializationSettings.SerializerSettings);
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
                return Result.Success(JObject.Parse(content));
            }
            catch (Exception ex)
            {
                _logger.LogPayfortError(content);
                return Result.Failure<JObject>($"{ex.Message} for '{content}'");
            }
        }


        private async Task<Result<string>> GetContent(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode
                ? Result.Success(content)
                : Result.Failure<string>(content);
        }


        private static string ToPayfortBoolean(bool value) => value ? "YES" : "NO";


        private static string ToPayfortAmount(decimal amount, Currencies currency)
            => decimal.ToInt64(amount * PayfortConstants.ExponentMultipliers[currency]).ToString();


        
        private readonly IHttpClientFactory _clientFactory;

        private readonly ILogger<PayfortService> _logger;
        private readonly PayfortOptions _options;
        private readonly IPayfortSignatureService _signatureService;
        private readonly IPayfortResponseParser _payfortResponseParser;
    }
}