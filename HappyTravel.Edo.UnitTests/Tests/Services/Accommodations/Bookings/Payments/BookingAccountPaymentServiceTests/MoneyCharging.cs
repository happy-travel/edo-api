using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Documents;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.Payments.BookingAccountPaymentServiceTests
{
    public class MoneyCharging
    {
        [Fact]
        public async Task Should_fail_if_payment_method_incorrect()
        {
            var accountPaymentService = CreateAccountPaymentService();
            var invalidPaymentMethodBooking = new Booking
            {
                PaymentMethod = PaymentMethods.CreditCard
            };

            var (_, isFailure, _) = await accountPaymentService.Charge(invalidPaymentMethodBooking, It.IsAny<ApiCaller>());

            Assert.True(isFailure);


            static BookingAccountPaymentService CreateAccountPaymentService()
                => new(Mock.Of<IAccountPaymentService>(),
                    Mock.Of<IBookingDocumentsService>(),
                    Mock.Of<IBookingPaymentCallbackService>(),
                    Mock.Of<ILogger<BookingAccountPaymentService>>(),
                    MockEdoContextFactory.Create().Object,
                    Mock.Of<IBookingDocumentsMailingService>());
        }


        [Fact]
        public async Task Should_call_account_payment_service()
        {
            var accountPaymentServiceMock = new Mock<IAccountPaymentService>();
            var bookingAccountPaymentService = CreateBookingAccountPaymentService(accountPaymentServiceMock.Object);
            var user = AgentContextFactory.CreateByAgentId(Agent.Id).ToUserInfo();

            await bookingAccountPaymentService.Charge(Booking, user);

            accountPaymentServiceMock
                .Verify(a => a.Charge("TEST_REF_CODE", user, It.IsAny<IBookingPaymentCallbackService>()),
                    Times.Once);
        }


        [Fact]
        public async Task Should_send_receipt()
        {
            var documentsServiceMock = new Mock<IBookingDocumentsService>();
            var documentsMailingServiceMock = new Mock<IBookingDocumentsMailingService>();
            var bookingAccountPaymentService = CreateBookingAccountPaymentService(documentsService: documentsServiceMock.Object,
                documentsMailingService: documentsMailingServiceMock.Object);

            await bookingAccountPaymentService.Charge(Booking, It.IsAny<ApiCaller>());

            documentsServiceMock
                .Verify(d => d.GenerateReceipt(It.IsAny<Booking>()), Times.Once);

            documentsMailingServiceMock
                .Verify(d => d.SendReceiptToCustomer(It.IsAny<(DocumentRegistrationInfo, PaymentReceipt)>(), "test_agent@test.com"));
        }


        static BookingAccountPaymentService CreateBookingAccountPaymentService(IAccountPaymentService? paymentService = null,
            IBookingDocumentsService? documentsService = null,
            IBookingDocumentsMailingService? documentsMailingService = null)
        {
            return new(paymentService ?? Mock.Of<IAccountPaymentService>(),
                documentsService ?? Mock.Of<IBookingDocumentsService>(),
                Mock.Of<IBookingPaymentCallbackService>(),
                Mock.Of<ILogger<BookingAccountPaymentService>>(),
                CreateEdoContext(),
                documentsMailingService ?? Mock.Of<IBookingDocumentsMailingService>());
            
            static EdoContext CreateEdoContext()
            {
                var edoContextMock = MockEdoContextFactory.Create();
                edoContextMock.Setup(x => x.Agents).Returns(DbSetMockProvider.GetDbSetMock(new[] {Agent}));
                return edoContextMock.Object;
            }
        }


        private static Agent Agent
            => new()
            {
                Id = 111,
                Email = "test_agent@test.com"
            };

        private static Booking Booking
            => new()
            {
                Id = 115,
                ReferenceCode = "TEST_REF_CODE",
                PaymentMethod = PaymentMethods.BankTransfer,
                AgentId = 111
            };
    }
}