using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.External.PaymentLinks.LinksProcessing
{
    public class SignatureCalculation
    {
        static SignatureCalculation()
        {
            LinkServiceMock = new Mock<IPaymentLinkService>();
            LinkServiceMock.Setup(s => s.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(Result.Ok(LinkData)));

            SignatureServiceMock = new Mock<IPayfortSignatureService>();
            SignatureServiceMock
                .Setup(s => s.Calculate(It.IsAny<Dictionary<string, string>>(), SignatureTypes.Request))
                .Callback<IDictionary<string, string>, SignatureTypes>((dictionary, requestType) => _dataToCalculateSignature = dictionary)
                .Returns(Result.Ok(TestSignature));
        }


        public SignatureCalculation(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }
        
        [Fact]
        public async Task Should_return_value_from_signature_service()
        {
            var processingService = CreateProcessingService();
            var (_, isFailure, signature, _) = await processingService.CalculateSignature(It.IsAny<string>(), MerchantReference, DeviceFingerprint, "en");

            Assert.False(isFailure);
            Assert.Equal(TestSignature, signature);
        }


        [Fact]
        public async Task Should_use_link_merchant_reference_for_signature()
        {
            var processingService = CreateProcessingService();
            await processingService.CalculateSignature(It.IsAny<string>(), MerchantReference, It.IsAny<string>(), "en");

            Assert.Equal(MerchantReference, _dataToCalculateSignature["merchant_reference"]);
        }
        
        [Fact]
        public async Task Should_use_fingerprint_for_signature()
        {
            var processingService = CreateProcessingService();
            await processingService.CalculateSignature(It.IsAny<string>(), It.IsAny<string>(), DeviceFingerprint, "en");

            Assert.Equal(DeviceFingerprint, _dataToCalculateSignature["device_fingerprint"]);
        }


        private PaymentLinksProcessingService CreateProcessingService()
            => new PaymentLinksProcessingService(Mock.Of<IPayfortService>(),
                LinkServiceMock.Object,
                SignatureServiceMock.Object,
                EmptyPayfortOptions,
                NotificationServiceMock,
                _dateTimeProvider,
                Mock.Of<IEntityLocker>());


        private static readonly IOptions<PayfortOptions> EmptyPayfortOptions = Options.Create(new PayfortOptions());

        private static readonly PaymentLinkData LinkData = new PaymentLinkData((decimal) 100.1, "test@test.com", ServiceTypes.HTL, Currencies.AED, "comment",
            ReferenceCode, CreditCardPaymentStatuses.Created);

        private static readonly IPaymentNotificationService NotificationServiceMock = Mock.Of<IPaymentNotificationService>();
        
        private readonly IDateTimeProvider _dateTimeProvider;

        private static readonly Mock<IPayfortSignatureService> SignatureServiceMock;
        private static readonly Mock<IPaymentLinkService> LinkServiceMock;
        private static IDictionary<string, string> _dataToCalculateSignature;
        private const string TestSignature = "test_signature";
        private const string MerchantReference = "d91f5fd2-91e3-4c04-bdf9-6ca690abe64a";
        private const string DeviceFingerprint = "aa7ed763-c290-4c73-bc11-dc2e70564592";
        private const string ReferenceCode = "HTL-000X2";
    }
}