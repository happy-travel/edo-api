using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.PaymentLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks
{
    public class PaymentLinksStorage : IPaymentLinksStorage
    {
        public PaymentLinksStorage(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            IOptions<PaymentLinkOptions> paymentLinkOptions,
            ITagProcessor tagProcessor)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _tagProcessor = tagProcessor;
            _paymentLinkOptions = paymentLinkOptions.Value;
        }


        public Task<Result<PaymentLink>> Get(string code)
        {
            const string invalidCodeError = "Invalid link code";
            return ValidateCode()
                .Bind(GetLink);


            Result ValidateCode()
            {
                if (code.Length != CodeLength)
                    return Result.Failure(invalidCodeError);

                var binaryData = Base64UrlEncoder.DecodeBytes(code);
                var isParsed = Guid.TryParse(BitConverter.ToString(binaryData).Replace("-", string.Empty), out _);

                return isParsed ? Result.Success() : Result.Failure(invalidCodeError);
            }


            async Task<Result<PaymentLink>> GetLink()
            {
                var link = await _context.PaymentLinks.SingleOrDefaultAsync(p => p.Code == code);
                return link == default
                    ? Result.Failure<PaymentLink>(invalidCodeError)
                    : Result.Success(link);
            }
        }


        public Task<Result<PaymentLink>> Register(PaymentLinkCreationRequest paymentLinkCreationData)
        {
            return Validate(paymentLinkCreationData)
                .Map(CreateLink);


            Result Validate(PaymentLinkCreationRequest linkData)
            {
                var linkSettings = _paymentLinkOptions.ClientSettings;
                return GenericValidator<PaymentLinkCreationRequest>.Validate(v =>
                {
                    v.RuleFor(data => data.ServiceType).IsInEnum();
                    v.RuleFor(data => data.Currency).IsInEnum();
                    v.RuleFor(data => data.Amount).GreaterThan(decimal.Zero);
                    v.RuleFor(data => data.Email).EmailAddress();
                    v.RuleFor(data => data.Comment).NotEmpty();

                    v.RuleFor(data => data.Currency)
                        .Must(linkSettings.Currencies.Contains);

                    v.RuleFor(data => data.ServiceType)
                        .Must(serviceType => linkSettings.ServiceTypes.ContainsKey(serviceType));
                }, linkData);
            }

            async Task<PaymentLink> CreateLink()
            {
                var referenceCode = await _tagProcessor.GenerateNonSequentialReferenceCode(paymentLinkCreationData.ServiceType, LinkDestinationCode);
                var paymentLink = new PaymentLink
                {
                    Email = paymentLinkCreationData.Email,
                    Amount = paymentLinkCreationData.Amount,
                    Currency = paymentLinkCreationData.Currency,
                    ServiceType = paymentLinkCreationData.ServiceType,
                    Comment = paymentLinkCreationData.Comment,
                    Created = _dateTimeProvider.UtcNow(),
                    Code = Base64UrlEncoder.Encode(Guid.NewGuid().ToByteArray()),
                    ReferenceCode = referenceCode
                };
                _context.PaymentLinks.Add(paymentLink);
                await _context.SaveChangesAsync();

                return paymentLink;
            }
            
            
        }


        public Task<Result> UpdatePaymentStatus(string code, PaymentResponse paymentResponse)
        {
            return Get(code)
                .Bind(UpdateLinkPaymentData);


            async Task<Result> UpdateLinkPaymentData(PaymentLink paymentLink)
            {
                paymentLink.LastPaymentResponse = JsonConvert.SerializeObject(paymentResponse);
                paymentLink.LastPaymentDate = _dateTimeProvider.UtcNow();
                _context.Update(paymentLink);
                await _context.SaveChangesAsync();

                return Result.Success();
            }
        }


        private const string LinkDestinationCode = "LNK";

        private static readonly int CodeLength = Base64UrlEncoder.Encode(Guid.Empty.ToByteArray()).Length;
        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITagProcessor _tagProcessor;
        private readonly PaymentLinkOptions _paymentLinkOptions;
    }
}