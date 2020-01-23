using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Converters;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.PaymentLinks;
using HappyTravel.MailSender;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using static HappyTravel.MailSender.Formatters.EmailContentFormatter;

namespace HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks
{
    public class PaymentLinkService : IPaymentLinkService
    {
        public PaymentLinkService(EdoContext context,
            IOptions<PaymentLinkOptions> options,
            IMailSender mailSender,
            IDateTimeProvider dateTimeProvider,
            IJsonSerializer jsonSerializer,
            ITagProcessor tagProcessor,
            ILogger<PaymentLinkService> logger)
        {
            _context = context;
            _mailSender = mailSender;
            _dateTimeProvider = dateTimeProvider;
            _jsonSerializer = jsonSerializer;
            _tagProcessor = tagProcessor;
            _logger = logger;
            _paymentLinkOptions = options.Value;
        }


        public Task<Result> Send(PaymentLinkData paymentLinkData)
        {
            return GenerateUri(paymentLinkData)
                .OnSuccess(SendMail)
                .OnBoth(WriteLog);


            Task<Result> SendMail(Uri url)
            {
                var payload = new
                {
                    amount = FromAmount(paymentLinkData.Amount, paymentLinkData.Currency),
                    comment = paymentLinkData.Comment,
                    paymentLink = url.ToString(),
                    serviceDescription = FromEnumDescription(paymentLinkData.ServiceType)
                };

                return _mailSender.Send(_paymentLinkOptions.MailTemplateId, paymentLinkData.Email, payload);
            }


            Result WriteLog(Result result)
            {
                if (result.IsFailure)
                    _logger.LogExternalPaymentLinkSendFailed($"Error sending email to {paymentLinkData.Email}: {result.Error}");
                else
                    _logger.LogExternalPaymentLinkSendSuccess($"Successfully sent e-mail to {paymentLinkData.Email}");

                return result;
            }
        }


        public Task<Result<Uri>> GenerateUri(PaymentLinkData paymentLinkData)
        {
            return ValidatePaymentData()
                .OnSuccess(GenerateLinkCode)
                .OnSuccess(StoreLink)
                .OnSuccess(GeneratePaymentUri)
                .OnBoth(WriteLog);


            Result ValidatePaymentData()
            {
                var linkSettings = _paymentLinkOptions.ClientSettings;
                return GenericValidator<PaymentLinkData>.Validate(v =>
                {
                    v.RuleFor(data => data.ServiceType).IsInEnum();
                    v.RuleFor(data => data.Currency).IsInEnum();
                    v.RuleFor(data => data.Amount).GreaterThan(decimal.Zero);
                    v.RuleFor(data => data.Email).EmailAddress();

                    v.RuleFor(data => data.Currency)
                        .Must(linkSettings.Currencies.Contains);

                    v.RuleFor(data => data.ServiceType)
                        .Must(serviceType => linkSettings.ServiceTypes.Keys.Contains(serviceType));
                }, paymentLinkData);
            }


            string GenerateLinkCode() => Base64UrlEncoder.Encode(Guid.NewGuid().ToByteArray());


            async Task<PaymentLink> StoreLink(string code)
            {
                var referenceCode = await _tagProcessor.GenerateNonSequentialReferenceCode(paymentLinkData.ServiceType, LinkDestinationCode);
                var paymentLink = new PaymentLink
                {
                    Email = paymentLinkData.Email,
                    Amount = paymentLinkData.Amount,
                    Currency = paymentLinkData.Currency,
                    ServiceType = paymentLinkData.ServiceType,
                    Comment = paymentLinkData.Comment,
                    Created = _dateTimeProvider.UtcNow(),
                    Code = code,
                    ReferenceCode = referenceCode
                };
                _context.PaymentLinks.Add(paymentLink);
                await _context.SaveChangesAsync();

                return paymentLink;
            }


            Uri GeneratePaymentUri(PaymentLink link) => new Uri($"{_paymentLinkOptions.PaymentUrlPrefix}/{link.Code}");


            Result<Uri> WriteLog(Result<Uri> result)
            {
                if (result.IsFailure)
                    _logger.LogExternalPaymentLinkSendFailed($"Error generating payment url for {paymentLinkData.Email}: {result.Error}");
                else
                    _logger.LogExternalPaymentLinkSendSuccess($"Successfully generated payment url for {paymentLinkData.Email}");

                return result;
            }
        }


        public ClientSettings GetClientSettings() => _paymentLinkOptions.ClientSettings;

        public List<Version> GetSupportedVersions() => _paymentLinkOptions.SupportedVersions;


        public Task<Result<PaymentLinkData>> Get(string code)
        {
            return GetLink(code)
                .OnSuccess(ToLinkData);


            PaymentLinkData ToLinkData(PaymentLink link)
            {
                var paymentStatus = string.IsNullOrWhiteSpace(link.LastPaymentResponse)
                    ? CreditCardPaymentStatuses.Created
                    : _jsonSerializer.DeserializeObject<PaymentResponse>(link.LastPaymentResponse).Status;

                return new PaymentLinkData(link.Amount, link.Email, link.ServiceType, link.Currency, link.Comment, link.ReferenceCode, paymentStatus);
            }
        }


        public Task<Result> UpdatePaymentStatus(string code, PaymentResponse paymentResponse)
        {
            return GetLink(code)
                .OnSuccess(UpdateLinkPaymentData);


            async Task<Result> UpdateLinkPaymentData(PaymentLink paymentLink)
            {
                paymentLink.LastPaymentResponse = _jsonSerializer.SerializeObject(paymentResponse);
                paymentLink.LastPaymentDate = _dateTimeProvider.UtcNow();
                _context.Update(paymentLink);
                await _context.SaveChangesAsync();

                return Result.Ok();
            }
        }


        private Task<Result<PaymentLink>> GetLink(string code)
        {
            const string invalidCodeError = "Invalid link code";
            return ValidateCode()
                .OnSuccess(GetLink);


            Result ValidateCode()
            {
                if (code.Length != CodeLength)
                    return Result.Fail(invalidCodeError);

                var binaryData = Base64UrlEncoder.DecodeBytes(code);
                var isParsed = Guid.TryParse(BitConverter.ToString(binaryData).Replace("-", string.Empty), out _);

                return isParsed ? Result.Ok() : Result.Fail(invalidCodeError);
            }


            async Task<Result<PaymentLink>> GetLink()
            {
                var link = await _context.PaymentLinks.SingleOrDefaultAsync(p => p.Code == code);
                return link == default
                    ? Result.Fail<PaymentLink>(invalidCodeError)
                    : Result.Ok(link);
            }
        }


        private const string LinkDestinationCode = "LNK";
        private static readonly int CodeLength = Base64UrlEncoder.Encode(Guid.Empty.ToByteArray()).Length;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger<PaymentLinkService> _logger;
        private readonly IMailSender _mailSender;
        private readonly PaymentLinkOptions _paymentLinkOptions;
        private readonly ITagProcessor _tagProcessor;
    }
}