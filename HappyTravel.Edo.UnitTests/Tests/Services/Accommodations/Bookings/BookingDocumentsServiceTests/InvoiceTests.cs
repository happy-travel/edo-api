using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Bookings;
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
using HappyTravel.Edo.Data.Documents;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Models;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingDocumentsServiceTests
{
    public class InvoiceTests
    {
        [Theory(Skip = "Can't test this, because check disabled")]
        [InlineData(BookingStatuses.Cancelled)]
        [InlineData(BookingStatuses.Rejected)]
        public async Task When_booking_has_not_allowed_status_generation_invoice_should_fail(BookingStatuses status)
        {
            var agentContext = AgentInfoFactory.GetByAgentId(1);
            var bookingDocumentsService = CreateBookingDocumentsService(new Booking
            {
                Id = 1,
                AgentId = 1,
                Status = status
            }, true);

            var (isSuccess, _) = await bookingDocumentsService.GetActualInvoice(1, agentContext.AgentId);

            Assert.False(isSuccess);
        }


        [Theory]
        [InlineData(BookingStatuses.Confirmed)]
        [InlineData(BookingStatuses.Invalid)]
        [InlineData(BookingStatuses.Pending)]
        [InlineData(BookingStatuses.Reverted)]
        [InlineData(BookingStatuses.InternalProcessing)]
        [InlineData(BookingStatuses.ManualCorrectionNeeded)]
        [InlineData(BookingStatuses.WaitingForResponse)]
        public async Task When_booking_has_allowed_status_generation_invoice_should_succeed(BookingStatuses status)
        {
            var agentContext = AgentInfoFactory.GetByAgentId(1);
            var bookingDocumentsService = CreateBookingDocumentsService(new Booking
            {
                Id = 1,
                AgentId = 1,
                Status = status
            }, true);

            var (isSuccess, _) = await bookingDocumentsService.GetActualInvoice(1, agentContext.AgentId);

            Assert.True(isSuccess);
        }


        [Fact]
        public async Task When_invoice_not_found_should_fail()
        {
            var agentContext = AgentInfoFactory.GetByAgentId(1);
            var bookingDocumentsService = CreateBookingDocumentsService(new Booking
            {
                Id = 1,
                AgentId = 1,
                Status = BookingStatuses.Pending
            }, false);

            var (isSuccess, _) = await bookingDocumentsService.GetActualInvoice(1, agentContext.AgentId);

            Assert.False(isSuccess);
        }


        private static BookingDocumentsService CreateBookingDocumentsService(Booking booking, bool hasInvoices)
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

            var invoices = hasInvoices
                ? new List<(DocumentRegistrationInfo Metadata, BookingInvoiceData Data)>
                {
                    (
                        new DocumentRegistrationInfo(It.IsAny<string>(), It.IsAny<DateTime>()),
                        new BookingInvoiceData(
                            new BookingInvoiceData.BuyerInfo(),
                            new BookingInvoiceData.SellerInfo(),
                            It.IsAny<string>(),
                            new List<BookingInvoiceData.InvoiceItemInfo>(),
                            new MoneyAmount(),
                            It.IsAny<DateTime>(),
                            It.IsAny<DateTime>(),
                            It.IsAny<DateTime>(),
                            It.IsAny<BookingPaymentStatuses>(),
                            It.IsAny<DateTime?>())
                    )
                }
                : new List<(DocumentRegistrationInfo Metadata, BookingInvoiceData Data)>();

            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(i => i.Get<BookingInvoiceData>(It.IsAny<ServiceTypes>(), It.IsAny<ServiceSource>(), It.IsAny<string>()))
                .ReturnsAsync(invoices);

            return new BookingDocumentsService(
                edoContext.Object,
                Mock.Of<IOptions<BankDetails>>(),
                bookingRecordManager,
                Mock.Of<IAccommodationService>(),
                Mock.Of<ICounterpartyService>(),
                invoiceServiceMock.Object,
                Mock.Of<IReceiptService>(),
                Mock.Of<IImageFileService>());
        }
    }
}