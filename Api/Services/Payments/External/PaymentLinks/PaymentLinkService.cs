using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Data.PaymentLinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks
{
    public class PaymentLinkService : IPaymentLinkService
    {
        public PaymentLinkService(IOptions<PaymentLinkOptions> options,
            IPaymentLinkNotificationService notificationService,
            IPaymentLinksStorage storage,
            ILogger<PaymentLinkService> logger)
        {
            _notificationService = notificationService;
            _storage = storage;
            _logger = logger;
            _paymentLinkOptions = options.Value;
        }


        public Task<Result> Send(PaymentLinkCreationRequest paymentLinkCreationData)
        {
            return CreateLink(paymentLinkCreationData)
                .Bind(SendMail)
                .Finally(WriteLog);


            Task<Result> SendMail(PaymentLinkData link)
            {
                var paymentUrl = GeneratePaymentUri(link).ToString();
                return _notificationService.SendLink(link, paymentUrl);
            }
            
            
            Result WriteLog(Result result)
            {
                if (result.IsFailure)
                    _logger.LogExternalPaymentLinkSendFailed(paymentLinkCreationData.Email, result.Error);
                else
                    _logger.LogExternalPaymentLinkSendSuccess(paymentLinkCreationData.Email);

                return result;
            }
        }

        public Task<Result<Uri>> GenerateUri(PaymentLinkCreationRequest paymentLinkCreationData)
        {
            return CreateLink(paymentLinkCreationData)
                .Map(GeneratePaymentUri);
        }
        
        
        public ClientSettings GetClientSettings() => _paymentLinkOptions.ClientSettings;

        public List<Version> GetSupportedVersions() => _paymentLinkOptions.SupportedVersions;


        public Task<Result<PaymentLinkData>> Get(string code)
        {
            return _storage.Get(code)
                .Map(PaymentLinkExtensions.ToLinkData);
        }

        
        private Task<Result<PaymentLinkData>> CreateLink(PaymentLinkCreationRequest paymentLinkCreationData)
        {
            return RegisterLink()
                .Map(PaymentLinkExtensions.ToLinkData)
                .Finally(WriteLog);

            
            Task<Result<PaymentLink>> RegisterLink() => _storage.Register(paymentLinkCreationData);
            
            Result<PaymentLinkData> WriteLog(Result<PaymentLinkData> result)
            {
                if (result.IsFailure)
                    _logger.LogExternalPaymentLinkGenerationFailed(paymentLinkCreationData.Email, result.Error);
                else
                    _logger.LogExternalPaymentLinkGenerationSuccess(paymentLinkCreationData.Email);

                return result;
            }
        }


        private Uri GeneratePaymentUri(PaymentLinkData link) => new Uri($"{_paymentLinkOptions.PaymentUrlPrefix}/{link.Code}");

        private readonly ILogger<PaymentLinkService> _logger;
        private readonly PaymentLinkOptions _paymentLinkOptions;
        private readonly IPaymentLinkNotificationService _notificationService;
        private readonly IPaymentLinksStorage _storage;
    }
}