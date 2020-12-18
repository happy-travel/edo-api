using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Documents;
using HappyTravel.Edo.Api.Services.Files;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.UnitTests.Utility;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingDocumentsServiceTests
{
    public class VoucherTests
    {
        [Fact]
        public async Task When_booking_has_not_confirmed_status_generation_voucher_should_fail()
        {
            var agentContext = AgentInfoFactory.GetByAgentId(1);
            var bookingDocumentsService = CreateBookingDocumentsService(new Booking
            {
                Id = 1,
                AgentId = 1,
                Status = BookingStatuses.Cancelled,
                PaymentStatus = It.IsAny<BookingPaymentStatuses>()
            });

            var (isSuccess, _) = await bookingDocumentsService.GenerateVoucher(1, agentContext, default);

            Assert.False(isSuccess);
        }

        [Fact]
        public async Task When_booking_has_confirmed_status_and_not_payed_generation_voucher_should_fail()
        {
            var agentContext = AgentInfoFactory.GetByAgentId(1);
            var bookingDocumentsService = CreateBookingDocumentsService(new Booking
            {
                Id = 1,
                AgentId = 1,
                Status = BookingStatuses.Confirmed,
                PaymentStatus = BookingPaymentStatuses.NotPaid
            });

            var (isSuccess, _) = await bookingDocumentsService.GenerateVoucher(1, agentContext, default);

            Assert.False(isSuccess);
        }


        [Fact]
        public async Task When_booking_has_confirmed_status_and_payed_generation_voucher_should_succeed()
        {
            var agentContext = AgentInfoFactory.GetByAgentId(1);
            var bookingDocumentsService = CreateBookingDocumentsService(new Booking
            {
                Id = 1,
                AgentId = 1,
                Status = BookingStatuses.Confirmed,
                PaymentStatus = BookingPaymentStatuses.Authorized
            });

            var (isSuccess, _) = await bookingDocumentsService.GenerateVoucher(1, agentContext, default);

            Assert.True(isSuccess);
        }


        private static BookingDocumentsService CreateBookingDocumentsService(Booking booking)
        {
            // If property is not initialized thrown NullReferenceException
            booking.Rooms = new List<BookedRoom>();

            var edoContext = MockEdoContextFactory.Create();
            edoContext.Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(new List<Booking>{booking}));

            var bookingRecordManager = new BookingRecordsManager(
                edoContext.Object,
                Mock.Of<IDateTimeProvider>(),
                Mock.Of<ITagProcessor>(),
                Mock.Of<IAccommodationService>(),
                Mock.Of<IAccommodationBookingSettingsService>(),
                Mock.Of<IAppliedBookingMarkupRecordsManager>(),
                Mock.Of<ISupplierOrderService>());

            return new BookingDocumentsService(
                edoContext.Object,
                Mock.Of<IOptions<BankDetails>>(),
                bookingRecordManager,
                Mock.Of<IAccommodationService>(),
                Mock.Of<ICounterpartyService>(),
                Mock.Of<IInvoiceService>(),
                Mock.Of<IReceiptService>(),
                Mock.Of<IImageFileService>());
        }
    }
}