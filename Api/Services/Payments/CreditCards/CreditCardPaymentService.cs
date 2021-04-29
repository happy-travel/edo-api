using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.CreditCards;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Payments;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public class CreditCardPaymentProcessingService : ICreditCardPaymentProcessingService
    {
        public CreditCardPaymentProcessingService(IPayfortResponseParser responseParser,
            EdoContext context,
            ICreditCardsManagementService creditCardsManagementService,
            IEntityLocker locker,
            IDateTimeProvider dateTimeProvider,
            ICreditCardMoneyAuthorizationService moneyAuthorizationService,
            ICreditCardMoneyCaptureService captureService,
            ICreditCardMoneyRefundService refundService)
        {
            _responseParser = responseParser;
            _context = context;
            _creditCardsManagementService = creditCardsManagementService;
            _locker = locker;
            _dateTimeProvider = dateTimeProvider;
            _moneyAuthorizationService = moneyAuthorizationService;
            _captureService = captureService;
            _refundService = refundService;
        }
        
        
        public async Task<Result<PaymentResponse>> Authorize(NewCreditCardPaymentRequest request, 
            string languageCode, string ipAddress, IPaymentCallbackService paymentCallbackService, AgentContext agent)
        {
            var (_, isFailure, servicePrice, error) = await paymentCallbackService.GetChargingAmount(request.ReferenceCode);
            if (isFailure)
                return Result.Failure<PaymentResponse>(error);
            
            return await Validate(request)
                .Bind(Authorize)
                .TapIf(IsSaveCardNeeded, SaveCard)
                .Bind(StorePaymentResults);;


            static Result Validate(NewCreditCardPaymentRequest payment)
            {
                return GenericValidator<NewCreditCardPaymentRequest>
                    .Validate(p =>
                    {
                        if (payment.IsSaveCardNeeded)
                        {
                            p.RuleFor(r => r.CardInfo)
                                .ChildRules(c =>
                                {
                                    c.RuleFor(info => info.HolderName)
                                        .NotEmpty()
                                        .WithMessage($"{nameof(CreditCardInfo.HolderName)} is required to save card");
                                });
                        }
                    }, payment);
            }

            async Task<Result<CreditCardPaymentResult>> Authorize()
            {
                var tokenType = request.IsSaveCardNeeded
                    ? PaymentTokenTypes.Stored
                    : PaymentTokenTypes.OneTime;

                var cardPaymentRequest = await CreatePaymentRequest(servicePrice,
                    new PaymentTokenInfo(request.Token, tokenType),
                    ipAddress, request.ReferenceCode,
                    languageCode, agent);

                return await _moneyAuthorizationService.AuthorizeMoneyForService(cardPaymentRequest, agent);
            }
            
            bool IsSaveCardNeeded(CreditCardPaymentResult response)
                => request.IsSaveCardNeeded && response.Status != CreditCardPaymentStatuses.Failed;
            
            Task SaveCard(CreditCardPaymentResult _) 
                => _creditCardsManagementService.Save(request.CardInfo, request.Token, agent);
            
            async Task<Result<PaymentResponse>> StorePaymentResults(CreditCardPaymentResult paymentResult)
            {
                var card = request.IsSaveCardNeeded
                    ? await _context.CreditCards.SingleOrDefaultAsync(c => c.Token == request.Token)
                    : null;
                
                var payment = await CreatePayment(ipAddress, servicePrice, card?.Id, paymentResult);
                var (_, isFailure, error) = await paymentCallbackService.ProcessPaymentChanges(payment);

                return isFailure
                    ? Result.Failure<PaymentResponse>(error)
                    : Result.Success(paymentResult.ToPaymentResponse());
            }
        }
        
        
        public async Task<Result<PaymentResponse>> Authorize(SavedCreditCardPaymentRequest request, string languageCode, 
            string ipAddress, IPaymentCallbackService paymentCallbackService, AgentContext agent)
        {
            var (_, isFailure, servicePrice, error) = await paymentCallbackService.GetChargingAmount(request.ReferenceCode);
            if (isFailure)
                return Result.Failure<PaymentResponse>(error);

            return await Validate(request)
                .Bind(Authorize)
                .Bind(StorePaymentResults);
            
            static Result Validate(SavedCreditCardPaymentRequest payment)
            {
                return GenericValidator<SavedCreditCardPaymentRequest>
                    .Validate(p =>
                    {
                        p.RuleFor(p => p.CardId).NotEmpty();
                    }, payment);
            }

            async Task<Result<CreditCardPaymentResult>> Authorize()
            {
                var (_, isFailure, token, error) = await _creditCardsManagementService.GetToken(request.CardId, agent);
                if (isFailure)
                    return Result.Failure<CreditCardPaymentResult>(error);
                
                var cardPaymentRequest = await CreatePaymentRequest(servicePrice,
                    new PaymentTokenInfo(token, PaymentTokenTypes.Stored),
                    ipAddress,
                    request.ReferenceCode,
                    languageCode,
                    agent,
                    request.SecurityCode);
                
                return await _moneyAuthorizationService.AuthorizeMoneyForService(cardPaymentRequest, agent);
            }
            
            async Task<Result<PaymentResponse>> StorePaymentResults(CreditCardPaymentResult paymentResult)
            {
                var payment = await CreatePayment(ipAddress, servicePrice, request.CardId, paymentResult);
                var (_, isFailure, error) = await paymentCallbackService.ProcessPaymentChanges(payment);

                return isFailure
                    ? Result.Failure<PaymentResponse>(error)
                    : Result.Success(paymentResult.ToPaymentResponse());
            }
        }
        

        private async Task<Payment> CreatePayment(string ipAddress, MoneyAmount moneyAmount,
            int? cardId, CreditCardPaymentResult paymentResult)
        {
            var now = _dateTimeProvider.UtcNow();
            var info = new CreditCardPaymentInfo(ipAddress, 
                paymentResult.ExternalCode,
                paymentResult.Message, 
                paymentResult.AuthorizationCode, 
                paymentResult.ExpirationDate,
                paymentResult.MerchantReference);

            var payment = new Payment
            {
                Amount = paymentResult.Amount,
                AccountNumber = paymentResult.CardNumber,
                Currency = moneyAmount.Currency.ToString(),
                Created = now,
                Modified = now,
                Status = paymentResult.Status.ToPaymentStatus(),
                Data = JsonConvert.SerializeObject(info),
                AccountId = cardId,
                PaymentMethod = PaymentTypes.CreditCard,
                ReferenceCode = paymentResult.ReferenceCode
            };
                
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }


        private async Task<CreditCardPaymentRequest> CreatePaymentRequest(MoneyAmount moneyAmount,
            PaymentTokenInfo paymentToken,
            string ipAddress,
            string referenceCode,
            string languageCode,
            AgentContext agent,
            string securityCode = default)
        {
            return new CreditCardPaymentRequest(currency: moneyAmount.Currency,
                amount: moneyAmount.Amount,
                token: paymentToken,
                customerName: $"{agent.FirstName} {agent.LastName}",
                customerEmail: agent.Email,
                customerIp: ipAddress,
                referenceCode: referenceCode,
                languageCode: languageCode,
                securityCode: securityCode,
                isNewCard: string.IsNullOrWhiteSpace(securityCode),
                merchantReference: await GetMerchantReference());


            async Task<string> GetMerchantReference()
            {
                var count = await _context.Payments.Where(p => p.ReferenceCode == referenceCode).CountAsync();
                return count == 0
                    ? referenceCode
                    : $"{referenceCode}-{count}";
            }
        }
        
        
        public async Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject rawResponse, IPaymentCallbackService paymentCallbackService)
        {
            var (_, isParseFailure, paymentResponse, parseError) = _responseParser.ParsePaymentResponse(rawResponse);
            if (isParseFailure)
                return Result.Failure<PaymentResponse>(parseError);

            return await Result.Success()
                .BindWithLock(_locker, typeof(Payment), paymentResponse.ReferenceCode, () => Result.Success()
                    .Bind(ProcessPaymentResponse)
                    .Map(StorePaymentResults));
            
            async Task<Result<(CreditCardPaymentResult, Payment)>> ProcessPaymentResponse()
            {
                var (_, isFailure, payment, error) = await GetPaymentForResponse(paymentResponse);
                if (isFailure)
                    return Result.Failure<(CreditCardPaymentResult, Payment)>(error);
                
                // Payment can be completed before. Nothing to do now.
                if (payment.Status == PaymentStatuses.Authorized)
                    return Result.Success((GetDefaultSuccessResult(), payment));

                var (_, _, serviceBuyer, _) = await paymentCallbackService.GetServiceBuyer(payment.ReferenceCode);
                
                var (_, isProcessFailure, processResult, processError) = await _moneyAuthorizationService.ProcessPaymentResponse(paymentResponse,
                    Enum.Parse<Currencies>(payment.Currency),
                    serviceBuyer.AgentId);
                
                if (isProcessFailure)
                    return Result.Failure<(CreditCardPaymentResult, Payment)>(processError);
                
                return Result.Success((processResult, payment));
                
                static CreditCardPaymentResult GetDefaultSuccessResult() => new CreditCardPaymentResult(string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    CreditCardPaymentStatuses.Success,
                    string.Empty,
                    default,
                    string.Empty);
            }
            
            
            async Task<PaymentResponse> StorePaymentResults((CreditCardPaymentResult, Payment) result)
            {
                var (paymentResult, payment) = result;
                // Payment can be completed before. Nothing to do now.
                if (payment.Status == PaymentStatuses.Authorized)
                    return paymentResult.ToPaymentResponse();
                
                var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                var newInfo = new CreditCardPaymentInfo(info.CustomerIp, paymentResult.ExternalCode, paymentResult.Message, paymentResult.AuthorizationCode,
                    paymentResult.ExpirationDate, info.InternalReferenceCode);
                payment.Status = paymentResult.Status.ToPaymentStatus();
                payment.Data = JsonConvert.SerializeObject(newInfo);
                payment.Modified = _dateTimeProvider.UtcNow();
                _context.Update(payment);
                await _context.SaveChangesAsync();
                await paymentCallbackService.ProcessPaymentChanges(payment);

                return paymentResult.ToPaymentResponse();
            }

            async Task<Result<Payment>> GetPaymentForResponse(CreditCardPaymentResult paymentResponse)
            {
                var payment = await _context.Payments
                    .SingleOrDefaultAsync(p => p.ReferenceCode == paymentResponse.ReferenceCode);

                if (payment == default)
                    return Result.Failure<Payment>($"Could not find a payment record with the reference code {paymentResponse.ReferenceCode}");

                if (payment.Amount != paymentResponse.Amount)
                    return Result.Failure<Payment>($"Invalid payment amount, expected: '{payment.Amount}', actual: '{paymentResponse.Amount}'");
                
                return Result.Success(payment);
            }
        }
        
        
        public async Task<Result<string>> CaptureMoney(string referenceCode, ApiCaller apiCaller, IPaymentCallbackService paymentCallbackService)
        {
            var payment = await _context.Payments
                .SingleOrDefaultAsync(p => p.ReferenceCode == referenceCode && p.Status == PaymentStatuses.Authorized);

            if (payment == default)
                return Result.Failure<string>($"Could not find payment by reference code {referenceCode}");

            
            if (payment.PaymentMethod != PaymentTypes.CreditCard)
                return Result.Failure<string>($"Invalid payment method: {payment.PaymentMethod}");

            return await Capture()
                .Tap(StoreCaptureResults)
                .Finally(CreateResult);
            

            async Task<Result<CreditCardCaptureResult>> Capture()
            {
                var (_, isFailure, servicePrice, error) = await paymentCallbackService.GetChargingAmount(referenceCode);
                if (isFailure)
                    return Result.Failure<CreditCardCaptureResult>(error);
                
                var paymentInfo = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);

                var request = new CreditCardCaptureMoneyRequest(currency: servicePrice.Currency,
                    amount: servicePrice.Amount,
                    externalId: paymentInfo.ExternalId,
                    merchantReference: paymentInfo.InternalReferenceCode,
                    languageCode: "en");

                var (_, _, buyerInfo, _) = await paymentCallbackService.GetServiceBuyer(referenceCode);

                return await _captureService.Capture(request,
                    paymentInfo,
                    payment.AccountNumber,
                    Enum.Parse<Currencies>(payment.Currency),
                    apiCaller,
                    buyerInfo.AgentId);
            }
            
            
            async Task StoreCaptureResults(CreditCardCaptureResult captureResult)
            {
                payment.Status = PaymentStatuses.Captured;
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                await paymentCallbackService.ProcessPaymentChanges(payment);
            }

            Result<string> CreateResult(Result<CreditCardCaptureResult> result)
                => result.IsSuccess
                    ? Result.Success($"Payment for the payment '{payment.ReferenceCode}' completed.")
                    : Result.Failure<string>($"Unable to complete payment for the payment '{payment.ReferenceCode}'. Reason: {result.Error}");
        }
        
        
        public async Task<Result> VoidMoney(string referenceCode, ApiCaller apiCaller, IPaymentCallbackService paymentCallbackService)
        {
            var payment = await _context.Payments.SingleOrDefaultAsync(p => p.ReferenceCode == referenceCode);
            if (payment.PaymentMethod != PaymentTypes.CreditCard)
                return Result.Failure<string>($"Invalid payment method: {payment.PaymentMethod}");

            return await Void()
                .Tap(StoreVoidResults);

            async Task<Result<CreditCardVoidResult>> Void()
            {
                var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                var request = new CreditCardVoidMoneyRequest(
                    externalId: info.ExternalId,
                    merchantReference: info.InternalReferenceCode,
                    languageCode: "en");

                var (_, _, buyerInfo, _) = await paymentCallbackService.GetServiceBuyer(referenceCode);

                return await _captureService.Void(request,
                    info,
                    payment.AccountNumber,
                    new MoneyAmount(payment.Amount, Enum.Parse<Currencies>(payment.Currency)),
                    payment.ReferenceCode,
                    apiCaller,
                    buyerInfo.AgentId);
            }
            
            async Task StoreVoidResults(CreditCardVoidResult voidResult)
            {
                payment.Status = PaymentStatuses.Voided;
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                await paymentCallbackService.ProcessPaymentChanges(payment);
            }
        }


        public async Task<Result> RefundMoney(string referenceCode, ApiCaller apiCaller, DateTime operationDate,  IPaymentCallbackService paymentCallbackService)
        {
            var payment = await _context.Payments.SingleOrDefaultAsync(p => p.ReferenceCode == referenceCode);
            if (payment.PaymentMethod != PaymentTypes.CreditCard)
                return Result.Failure<string>($"Invalid payment method: {payment.PaymentMethod}");

            return await GetRefundableAmount()
                .Bind(Refund)
                .Tap(StoreRefundResults);


            Task<Result<MoneyAmount>> GetRefundableAmount() 
                => paymentCallbackService.GetRefundableAmount(referenceCode, operationDate);


            async Task<Result<CreditCardRefundResult>> Refund(MoneyAmount refundingAmount)
            {
                var info = JsonConvert.DeserializeObject<CreditCardPaymentInfo>(payment.Data);
                var request = new CreditCardRefundMoneyRequest(
                    currency: refundingAmount.Currency,
                    amount: refundingAmount.Amount,
                    externalId: info.ExternalId,
                    merchantReference: info.InternalReferenceCode,
                    languageCode: "en");

                var (_, _, buyerInfo, _) = await paymentCallbackService.GetServiceBuyer(referenceCode);

                return await _refundService.Refund(request,
                    info,
                    payment.AccountNumber,
                    payment.ReferenceCode,
                    apiCaller,
                    buyerInfo.AgentId);
            }


            async Task StoreRefundResults()
            {
                payment.Status = PaymentStatuses.Refunded;
                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();
                await paymentCallbackService.ProcessPaymentChanges(payment);
            }
        }


        private readonly IPayfortResponseParser _responseParser;
        private readonly EdoContext _context;
        private readonly ICreditCardsManagementService _creditCardsManagementService;
        private readonly IEntityLocker _locker;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICreditCardMoneyAuthorizationService _moneyAuthorizationService;
        private readonly ICreditCardMoneyCaptureService _captureService;
        private readonly ICreditCardMoneyRefundService _refundService;
    }
}