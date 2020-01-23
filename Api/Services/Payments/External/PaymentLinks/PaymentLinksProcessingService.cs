using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.PaymentLinks;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks
{
    public class PaymentLinksProcessingService : IPaymentLinksProcessingService
    {
        public PaymentLinksProcessingService(IPayfortService payfortService,
            IPaymentLinkService linkService,
            IPayfortSignatureService signatureService,
            IOptions<PayfortOptions> payfortOptions,
            IPaymentNotificationService notificationService,
            IDateTimeProvider dateTimeProvider,
            IEntityLocker locker)
        {
            _payfortService = payfortService;
            _linkService = linkService;
            _signatureService = signatureService;
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
            _locker = locker;
            _payfortOptions = payfortOptions.Value;
        }


        public Task<Result<PaymentResponse>> Pay(string code, string token, string ip, string languageCode)
        {
            return GetLink(code)
                .OnSuccess(link => ProcessPay(link, code, token, ip, languageCode));
        }


        public Task<Result<PaymentResponse>> ProcessResponse(string code, JObject response)
        {
            return LockLink()
                .OnSuccess(GetLink)
                .OnSuccess(ProcessResponse)
                .OnBoth(UnlockLink);

            Task<Result> LockLink() => _locker.Acquire<PaymentLink>(code, nameof(PaymentLinksProcessingService));

            Task<Result<PaymentLinkData>> GetLink() => this.GetLink(code);

            Task<Result<PaymentResponse>> ProcessResponse(PaymentLinkData link) => this.ProcessResponse(link, code, response);


            async Task<Result<PaymentResponse>> UnlockLink(Result<PaymentResponse> paymentResponse)
            {
                await _locker.Release<PaymentLink>(code);
                return paymentResponse;
            }
        }


        public Task<Result<string>> CalculateSignature(string code, string merchantReference, string fingerprint, string languageCode)
        {
            return GetLink(code)
                .OnSuccess(GetSignature);


            Result<string> GetSignature(PaymentLinkData paymentLinkData)
            {
                var signingData = new Dictionary<string, string>
                {
                    {"service_command", "TOKENIZATION"},
                    {"access_code", _payfortOptions.AccessCode},
                    {"merchant_identifier", _payfortOptions.Identifier},
                    {"merchant_reference", merchantReference},
                    {"language", languageCode},
                    {"device_fingerprint", fingerprint},
                    {"return_url", $"{_payfortOptions.ResultUrl}/{paymentLinkData.ReferenceCode}"},
                    {"signature", string.Empty}
                };
                return _signatureService.Calculate(signingData, SignatureTypes.Request);
            }
        }


        private Task<Result<PaymentResponse>> ProcessPay(PaymentLinkData link, string code, string token, string ip, string languageCode)
        {
            return Pay()
                .OnSuccessIf(IsPaymentComplete, SendBillToCustomer)
                .Map(ToPaymentResponse)
                .OnSuccess(StorePaymentResult);


            Task<Result<CreditCardPaymentResult>> Pay()
                => _payfortService.Pay(new CreditCardPaymentRequest(
                    link.Amount,
                    link.Currency,
                    new PaymentTokenInfo(token, PaymentTokenTypes.OneTime),
                    null,
                    link.Email,
                    ip,
                    link.ReferenceCode,
                    languageCode,
                    true,
                    // Is not needed for new card
                    null,
                    link.ReferenceCode));


            bool IsPaymentComplete(CreditCardPaymentResult paymentResult) => paymentResult.Status == CreditCardPaymentStatuses.Success;

            Task SendBillToCustomer() => this.SendBillToCustomer(link);

            PaymentResponse ToPaymentResponse(CreditCardPaymentResult cr) => new PaymentResponse(cr.Secure3d, cr.Status, cr.Message);

            Task StorePaymentResult(PaymentResponse response) => _linkService.UpdatePaymentStatus(code, response);
        }


        private Task<Result<PaymentResponse>> ProcessResponse(PaymentLinkData link, string code, JObject response)
        {
            return ParseResponse()
                .OnSuccessIf(IsLinkNotPaid, SendBillToCustomer)
                .OnSuccess(StorePaymentResult);


            Result<PaymentResponse> ParseResponse()
            {
                var (_, isFailure, cr, error) = _payfortService.ParsePaymentResponse(response);
                if (isFailure)
                    return Result.Fail<PaymentResponse>(error);

                return Result.Ok(new PaymentResponse(cr.Secure3d, cr.Status, cr.Message));
            }


            bool IsLinkNotPaid(PaymentResponse _) => link.CreditCardPaymentStatus != CreditCardPaymentStatuses.Success;

            Task SendBillToCustomer() => this.SendBillToCustomer(link);


            async Task<PaymentResponse> StorePaymentResult(PaymentResponse paymentResponse)
            {
                await _linkService.UpdatePaymentStatus(code, paymentResponse);
                return paymentResponse;
            }
        }


        private Task SendBillToCustomer(PaymentLinkData link)
            => _notificationService.SendBillToCustomer(new PaymentBill(
                link.Email,
                link.Amount,
                link.Currency,
                _dateTimeProvider.UtcNow(),
                PaymentMethods.CreditCard,
                link.ReferenceCode));


        private Task<Result<PaymentLinkData>> GetLink(string code) => _linkService.Get(code);

        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPaymentLinkService _linkService;
        private readonly IEntityLocker _locker;
        private readonly IPaymentNotificationService _notificationService;
        private readonly PayfortOptions _payfortOptions;

        private readonly IPayfortService _payfortService;
        private readonly IPayfortSignatureService _signatureService;
    }
}