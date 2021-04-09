using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Models;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public interface ICreditCardPaymentProcessingService
    {
        Task<Result<PaymentResponse>> Authorize(NewCreditCardPaymentRequest request, 
            string languageCode, string ipAddress, IPaymentCallbackService paymentCallbackService, AgentContext agent);


        Task<Result<PaymentResponse>> Authorize(SavedCreditCardPaymentRequest request, string languageCode, 
            string ipAddress, IPaymentCallbackService paymentCallbackService, AgentContext agent);


        Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject rawResponse, IPaymentCallbackService paymentCallbackService);

        Task<Result<string>> CaptureMoney(string referenceCode, ApiCaller apiCaller, IPaymentCallbackService paymentCallbackService);

        Task<Result> VoidMoney(string referenceCode, ApiCaller apiCaller, IPaymentCallbackService paymentCallbackService);

        Task<Result> RefundMoney(string referenceCode, ApiCaller apiCaller, DateTime operationDate, IPaymentCallbackService paymentCallbackService);
    }
}