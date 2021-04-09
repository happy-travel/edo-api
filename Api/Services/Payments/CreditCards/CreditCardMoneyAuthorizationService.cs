using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments.AuditEvents;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public class CreditCardMoneyAuthorizationService : ICreditCardMoneyAuthorizationService
    {
        public CreditCardMoneyAuthorizationService(IPayfortService payfortService,
            ICreditCardAuditService creditCardAuditService)
        {
            _payfortService = payfortService;
            _creditCardAuditService = creditCardAuditService;
        }


        public Task<Result<CreditCardPaymentResult>> ProcessPaymentResponse(CreditCardPaymentResult paymentResponse,
            Currencies currency,
            int agentId)
        {
            return CheckPaymentStatusNotFailed(paymentResponse)
                .TapIf(IsPaymentComplete, cardPaymentResult => WriteAuditLog());


            Result<CreditCardPaymentResult> CheckPaymentStatusNotFailed(CreditCardPaymentResult payment)
                => payment.Status == CreditCardPaymentStatuses.Failed
                    ? Result.Failure<CreditCardPaymentResult>($"Payment error: {payment.Message}")
                    : Result.Success(payment);


            bool IsPaymentComplete(CreditCardPaymentResult cardPaymentResult) => cardPaymentResult.Status == CreditCardPaymentStatuses.Success;

            Task WriteAuditLog() => WriteAuthorizeAuditLog(paymentResponse, agentId, currency);
        }


        public Task<Result<CreditCardPaymentResult>> AuthorizeMoneyForService(CreditCardPaymentRequest request,
            AgentContext agent)
        {
            return AuthorizeInPaymentSystem(request)
                .Tap(WriteAuditLog);


            async Task<Result<CreditCardPaymentResult>> AuthorizeInPaymentSystem(CreditCardPaymentRequest paymentRequest)
            {
                var (_, isFailure, paymentResult, error) = await _payfortService.Authorize(paymentRequest);
                if (isFailure)
                    return Result.Failure<CreditCardPaymentResult>(error);

                return paymentResult.Status == CreditCardPaymentStatuses.Failed
                    ? Result.Failure<CreditCardPaymentResult>($"Payment error: {paymentResult.Message}")
                    : Result.Success(paymentResult);
            }

            
            Task WriteAuditLog(CreditCardPaymentResult result) => WriteAuthorizeAuditLog(result, agent.AgentId, request.Currency);
        }



        private async Task WriteAuthorizeAuditLog(CreditCardPaymentResult payment, int agentId, Currencies currency)
        {
            // No need to log secure 3d request, audit log will be written when when secure 3d passes and actual authorization occurs
            if (payment.Status == CreditCardPaymentStatuses.Secure3d)
                return;

            var eventData = new CreditCardLogEventData($"Authorize money for the payment '{payment.ReferenceCode}'",
                payment.ExternalCode,
                payment.Message,
                payment.MerchantReference);

            await _creditCardAuditService.Write(CreditCardEventType.Authorize,
                payment.CardNumber,
                payment.Amount,
                new ApiCaller(agentId, ApiCallerTypes.Agent),
                eventData,
                payment.ReferenceCode,
                agentId,
                currency);
        }


        private readonly IPayfortService _payfortService;
        private readonly ICreditCardAuditService _creditCardAuditService;
    }
}