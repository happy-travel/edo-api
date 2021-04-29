using System;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.Notifications;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.UnitTests.Utility;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingRecordsUpdaterTests
{
    public class StatusChangeTests
    {
        public StatusChangeTests()
        {
            _notificationServiceMock = new Mock<IBookingNotificationService>();
            _documentsMailingServiceMock = new Mock<IBookingDocumentsMailingService>();
            _supplierOrderServiceMock = new Mock<ISupplierOrderService>();
            _bookingMoneyReturnServiceMock = new Mock<IBookingMoneyReturnService>();
        }
        
        
        [Fact]
        public async Task Confirmation_should_call_notification_and_sending_invoice()
        {
            var service = CreateBookingRecordsUpdaterService();

            await service.ChangeStatus(Bookings.First(), BookingStatuses.Confirmed, DateTime.UtcNow, ApiCaller, ChangeReason);
            
            _notificationServiceMock.Verify(x => x.NotifyBookingFinalized(It.IsAny<AccommodationBookingInfo>()));
            _documentsMailingServiceMock.Verify(x => x.SendInvoice(It.IsAny<Booking>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<SlimAgentContext>()));
        }

        
        [Fact]
        public async Task Cancellation_should_call_notification_cancelling_from_supplier_return_of_the_money()
        {
            var service = CreateBookingRecordsUpdaterService();

            await service.ChangeStatus(Bookings.First(), BookingStatuses.Cancelled, DateTime.UtcNow, ApiCaller, ChangeReason);
            
            _notificationServiceMock.Verify(x => x.NotifyBookingCancelled(It.IsAny<AccommodationBookingInfo>()));
            _supplierOrderServiceMock.Verify(x => x.Cancel(It.IsAny<string>())); 
            _bookingMoneyReturnServiceMock.Verify(x => x.ReturnMoney(It.IsAny<Booking>(), It.IsAny<DateTime>(), It.IsAny<ApiCaller>()));
        }


        [Theory]
        [InlineData(BookingStatuses.Rejected)]
        [InlineData(BookingStatuses.Invalid)]
        [InlineData(BookingStatuses.Discarded)]
        public async Task Discarding_should_cancel_from_supplier_and_return_the_money(BookingStatuses status)
        {
            var service = CreateBookingRecordsUpdaterService();

            await service.ChangeStatus(Bookings.First(), status, DateTime.UtcNow, ApiCaller, ChangeReason);
            
            _supplierOrderServiceMock.Verify(x => x.Cancel(It.IsAny<string>())); 
            _bookingMoneyReturnServiceMock.Verify(x => x.ReturnMoney(It.IsAny<Booking>(), It.IsAny<DateTime>(), It.IsAny<ApiCaller>()));
        }


        [Fact]
        public async Task Manual_correction_should_call_notification()
        {
            var service = CreateBookingRecordsUpdaterService();

            await service.ChangeStatus(Bookings.First(), BookingStatuses.ManualCorrectionNeeded, DateTime.UtcNow, ApiCaller, ChangeReason);
            
            _notificationServiceMock.Verify(x => x.NotifyBookingManualCorrectionNeeded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        
        private BookingRecordsUpdater CreateBookingRecordsUpdaterService()
        {
            var context = MockEdoContextFactory.Create();
            
            context
                .Setup(x => x.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(Bookings));

            context
                .Setup(x => x.Agents)
                .Returns(DbSetMockProvider.GetDbSetMock(Agents));

            context
                .Setup(x => x.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(Agencies));
            
            return new BookingRecordsUpdater(
                Mock.Of<IDateTimeProvider>(), 
                Mock.Of<IBookingInfoService>(), 
                _notificationServiceMock.Object,
                _bookingMoneyReturnServiceMock.Object,
                _documentsMailingServiceMock.Object,
                _supplierOrderServiceMock.Object,
                Mock.Of<ISendingNotificationsService>(),
                Mock.Of<IBookingChangeLogService>(),
                context.Object, 
                Mock.Of<ILogger<BookingRecordsUpdater>>());
        }
        
        
        private static readonly Booking[] Bookings =
        {
            new()
            {
                Id = 1, PaymentStatus = BookingPaymentStatuses.Authorized, Status = BookingStatuses.Pending, PaymentMethod = PaymentTypes.CreditCard, AgentId = 1, AgencyId = 1
            }
        };

        private static readonly Agent[] Agents =
        {
            new()
            {
                Id = 1
            }
        };

        private static readonly Agency[] Agencies =
        {
            new()
            {
                Id = 1
            }
        };

        private static readonly BookingChangeReason ChangeReason = new();

        private static readonly ApiCaller ApiCaller = new();
        
        private readonly Mock<IBookingNotificationService> _notificationServiceMock;
        private readonly Mock<IBookingDocumentsMailingService> _documentsMailingServiceMock;
        private readonly Mock<ISupplierOrderService> _supplierOrderServiceMock;
        private readonly Mock<IBookingMoneyReturnService> _bookingMoneyReturnServiceMock;
    }
}