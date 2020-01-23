using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.Payments.External.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments.Payfort;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.External.PaymentLinks.LinksProcessing
{
    public class PaymentProcess
    {
        private readonly IDateTimeProvider _dateTimeProvider;


        static PaymentProcess()
        {
            LinkServiceMock = new Mock<IPaymentLinkService>();
            LinkServiceMock.Setup(s => s.Get(It.IsAny<string>()))
                .Returns(Task.FromResult(Result.Ok(Links[0])));
            NotificationServiceMock = new Mock<IPaymentNotificationService>();
            EntityLockerMock = new Mock<IEntityLocker>();
            EntityLockerMock.Setup(l => l.Acquire<It.IsAnyType>(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(Result.Ok()));
        }


        


        public PaymentProcess(IDateTimeProvider dateTimeProvider)
        {
            _dateTimeProvider = dateTimeProvider;
        }

        [Theory]
        [MemberData(nameof(CreditCardPaymentResults))]
        public async Task Should_return_payment_result_from_payfort(CreditCardPaymentResult cardPaymentResult)
        {
            var processingService = new PaymentLinksProcessingService(CreatMockPayfortService(),
                LinkServiceMock.Object,
                SignatureServiceStub,
                EmptyPayfortOptions,
                NotificationServiceMock.Object,
                _dateTimeProvider,
                EntityLockerMock.Object);

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
            LinkServiceMock.Invocations.Clear();
            var processingService = CreateProcessingServiceWithProcess();

            const string linkCode = "fkkk4l88lll";
            var (_, _, response, _) = await processingService.ProcessResponse(linkCode, It.IsAny<JObject>());

            LinkServiceMock
                .Verify(l => l.UpdatePaymentStatus(linkCode, response), Times.Once);


            PaymentLinksProcessingService CreateProcessingServiceWithProcess()
            {
                var service = new Mock<IPayfortService>();
                service.Setup(p => p.ParsePaymentResponse(It.IsAny<JObject>()))
                    .Returns(Result.Ok(new CreditCardPaymentResult()));

                var paymentLinksProcessingService = new PaymentLinksProcessingService(service.Object,
                    LinkServiceMock.Object,
                    SignatureServiceStub,
                    EmptyPayfortOptions,
                    NotificationServiceMock.Object,
                    _dateTimeProvider,
                    EntityLockerMock.Object);

                return paymentLinksProcessingService;
            }
        }


        [Fact]
        public async Task Should_store_successful_payment_result()
        {
            var processingService = CreateProcessingServiceWithSuccessfulPay();

            const string linkCode = "fdf22dd237ll88lll";
            await processingService.Pay(linkCode, AnyString, "::1", "en");

            LinkServiceMock
                .Verify(l => l.UpdatePaymentStatus(linkCode, It.IsAny<PaymentResponse>()), Times.Once);


            PaymentLinksProcessingService CreateProcessingServiceWithSuccessfulPay()
            {
                var service = new Mock<IPayfortService>();
                service.Setup(p => p.Pay(It.IsAny<CreditCardPaymentRequest>()))
                    .Returns(Task.FromResult(Result.Ok(new CreditCardPaymentResult())));

                var paymentLinksProcessingService = new PaymentLinksProcessingService(service.Object,
                    LinkServiceMock.Object,
                    SignatureServiceStub,
                    EmptyPayfortOptions,
                    NotificationServiceMock.Object,
                    _dateTimeProvider,
                    EntityLockerMock.Object);
                return paymentLinksProcessingService;
            }
        }
        

        private static readonly string AnyString = It.IsAny<string>();

        private static readonly PaymentLinkData[] Links =
        {
            new PaymentLinkData(100.1m, "test@test.com", ServiceTypes.HTL, Currencies.AED, "comment", "HTL-000X2", CreditCardPaymentStatuses.Created)
        };

        private static readonly IPayfortSignatureService SignatureServiceStub = Mock.Of<IPayfortSignatureService>();
        private static readonly Mock<IPaymentLinkService> LinkServiceMock;
        private static readonly IOptions<PayfortOptions> EmptyPayfortOptions = Options.Create(new PayfortOptions());
        private static readonly Mock<IPaymentNotificationService> NotificationServiceMock;
        private static readonly Mock<IEntityLocker> EntityLockerMock;

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
    }
}