using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.PaymentLinks;
using HappyTravel.Edo.UnitTests.Stubs;
using HappyTravel.Money.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Payments.External.PaymentLinks.PaymentLinksProcessingServiceTests
{
    public class PaymentProcess
    {
        static PaymentProcess()
        {
            EntityLocker = new EntityLockerStub();
        }


        [Theory]
        [MemberData(nameof(CreditCardPaymentResults))]
        public async Task Should_return_payment_result_from_payfort(CreditCardPaymentResult cardPaymentResult)
        {
            var processingService = new PaymentLinksProcessingService(CreatMockPayfortService(),
                Mock.Of<IPayfortResponseParser>(),
                CreateLinkStorageMock().Object,
                SignatureServiceStub,
                EmptyPayfortOptions,
                Mock.Of<IPaymentLinkNotificationService>(),
                EntityLocker);

            var (_, isFailure, response, _) = await processingService.Pay(AnyString,
                AnyString, "::1",
                "en");

            Assert.False(isFailure);
            Assert.Equal(response.Status, cardPaymentResult.Status);
            Assert.Equal(response.Message, cardPaymentResult.Message);


            IPayfortService CreatMockPayfortService()
            {
                var service = new Mock<IPayfortService>();
                service.Setup(p => p.Pay(It.IsAny<CreditCardPaymentRequest>()))
                    .Returns(Task.FromResult(Result.Ok(cardPaymentResult)));

                return service.Object;
            }
        }

        
        [Fact]
        public async Task Should_store_successful_callback_result()
        {
            var linkStorageMock = CreateLinkStorageMock();
            var processingService = CreateProcessingServiceWithProcess();

            const string linkCode = "fkkk4l88lll";
            var (_, _, response, _) = await processingService.ProcessResponse(linkCode, It.IsAny<JObject>());

            linkStorageMock
                .Verify(l => l.UpdatePaymentStatus(linkCode, response), Times.Once);


            PaymentLinksProcessingService CreateProcessingServiceWithProcess()
            {
                var parser = new Mock<IPayfortResponseParser>();
                parser.Setup(p => p.ParsePaymentResponse(It.IsAny<JObject>()))
                    .Returns(Result.Ok(new CreditCardPaymentResult()));

                var paymentLinksProcessingService = new PaymentLinksProcessingService(
                    Mock.Of<IPayfortService>(),
                    parser.Object,
                    linkStorageMock.Object,
                    SignatureServiceStub,
                    EmptyPayfortOptions,
                    Mock.Of<IPaymentLinkNotificationService>(),
                    EntityLocker);

                return paymentLinksProcessingService;
            }
        }


        [Fact]
        public async Task Should_send_confirmation_on_successful_payment()
        {
            var notificationServiceMock = new Mock<IPaymentLinkNotificationService>(); 
            var processingService = CreateProcessingServiceWithSuccessfulPay(notificationServiceMock);

            const string linkCode = "fdf22dd237ll88lll";
            await processingService.Pay(linkCode, AnyString, "::1", "en");

            notificationServiceMock
                .Verify(l => l.SendPaymentConfirmation(It.IsAny<PaymentLinkData>()), Times.Once);


            static PaymentLinksProcessingService CreateProcessingServiceWithSuccessfulPay(Mock<IPaymentLinkNotificationService> notificationServiceMock)
            {
                var service = new Mock<IPayfortService>();
                service.Setup(p => p.Pay(It.IsAny<CreditCardPaymentRequest>()))
                    .Returns(Task.FromResult(Result.Ok(SuccessCreditCardPaymentResult)));

                var paymentLinksProcessingService = new PaymentLinksProcessingService(
                    service.Object,
                    Mock.Of<IPayfortResponseParser>(),
                    CreateLinkStorageMock().Object,
                    SignatureServiceStub,
                    EmptyPayfortOptions,
                    notificationServiceMock.Object,
                    EntityLocker);
                return paymentLinksProcessingService;
            }
        }
        
        
        [Fact]
        public async Task Should_store_successful_payment_result()
        {
            var linkStorageMock = CreateLinkStorageMock();
            var processingService = CreateProcessingServiceWithSuccessfulPay();
            const string linkCode = "fdf22dd237ll88lll";
            
            await processingService.Pay(linkCode, AnyString, "::1", "en");

            linkStorageMock
                .Verify(l => l.UpdatePaymentStatus(linkCode, It.IsAny<PaymentResponse>()), Times.Once);


            PaymentLinksProcessingService CreateProcessingServiceWithSuccessfulPay()
            {
                var service = new Mock<IPayfortService>();
                service.Setup(p => p.Pay(It.IsAny<CreditCardPaymentRequest>()))
                    .Returns(Task.FromResult(Result.Ok(new CreditCardPaymentResult())));

                var paymentLinksProcessingService = new PaymentLinksProcessingService(
                    service.Object,
                    Mock.Of<IPayfortResponseParser>(),
                    linkStorageMock.Object,
                    SignatureServiceStub,
                    EmptyPayfortOptions,
                    Mock.Of<IPaymentLinkNotificationService>(),
                    EntityLocker);
                return paymentLinksProcessingService;
            }
        }


        private static Mock<IPaymentLinksStorage> CreateLinkStorageMock()
        {
            var linkStorageMock = new Mock<IPaymentLinksStorage>();
            linkStorageMock.Setup(s => s.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(Result.Ok(Links[0])));
            return linkStorageMock;
        }


        private static readonly string AnyString = It.IsAny<string>();

        private static readonly PaymentLink[] Links =
        {
            new PaymentLink
                {
                    Amount = 100.1m,
                    Code = "someCode",
                    Comment = "comment",
                    Currency = Currencies.AED,
                    Email = "test@test.com",
                    ServiceType = ServiceTypes.HTL,
                    ReferenceCode = "HTL-000X2",
                }
        };

        private static readonly IPayfortSignatureService SignatureServiceStub = Mock.Of<IPayfortSignatureService>();
        private static readonly IOptions<PayfortOptions> EmptyPayfortOptions = Options.Create(new PayfortOptions());
        private static readonly IEntityLocker EntityLocker;

        public static object[][] CreditCardPaymentResults =
        {
            new object[]
            {
                new CreditCardPaymentResult(AnyString,
                    AnyString,
                    AnyString,
                    AnyString,
                    AnyString,
                    AnyString,
                    CreditCardPaymentStatuses.Created,
                    "Message1",
                    100.1m,
                    AnyString)
            },
            new object[]
            {
                new CreditCardPaymentResult(AnyString,
                    AnyString,
                    AnyString,
                    AnyString,
                    AnyString,
                    AnyString,
                    CreditCardPaymentStatuses.Success,
                    "Message2",
                    100.1m,
                    AnyString)
            }
        };

        private static readonly CreditCardPaymentResult SuccessCreditCardPaymentResult = new CreditCardPaymentResult(default, default, default, default,
            default, default, CreditCardPaymentStatuses.Success, default, 100.1m, default);
    }
}