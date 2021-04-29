using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.BatchProcessing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;
using HappyTravel.Edo.UnitTests.Utility;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingsProcessingServiceTests.DeadlineNotification
{
    public class ExecutingNotification
    {
        [Fact]
        public async Task All_bookings_should_be_notified_deadline()
        {
            var mailingServiceMock = new Mock<IBookingNotificationService>();
            var service = CreateProcessingService(mailingServiceMock.Object);

            await service.NotifyDeadlineApproaching(new List<int> {1, 2, 3}, new ServiceAccount {Id = 5, ClientId = "ClientId"});

            mailingServiceMock
                .Verify(
                    b => b.NotifyDeadlineApproaching(It.IsAny<int>(), AgentEmail),
                    Times.Exactly(3)
                );
        }
        
        
        private BookingsProcessingService CreateProcessingService(IBookingNotificationService notificationService)
        {
            var context = MockEdoContextFactory.Create();
            context.Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(Bookings));

            context.Setup(c => c.Agents)
                .Returns(DbSetMockProvider.GetDbSetMock(new[] {Agent}));

            var service = new BookingsProcessingService(Mock.Of<IBookingAccountPaymentService>(),
                Mock.Of<IBookingCreditCardPaymentService>(),
                Mock.Of<ISupplierBookingManagementService>(),
                notificationService,
                Mock.Of<IBookingReportsService>(),
                context.Object,
                Mock.Of<IBookingRecordsUpdater>(),
                Mock.Of<IDateTimeProvider>());
            return service;
        }


        private const int AgentId = 42;
        private const string AgentEmail = "agent@mail.com";

        private static readonly Agent Agent = new Agent {Id = AgentId, Email = AgentEmail};

        private static readonly Booking[] Bookings =
        {
            new Booking
            {
                Id = 1,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-222",
                Status = BookingStatuses.Pending,
                PaymentMethod = PaymentTypes.VirtualAccount,
                CheckInDate = new DateTime(2021, 12, 10),
                AgentId = AgentId
            },
            new Booking
            {
                Id = 2,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-223",
                Status = BookingStatuses.Confirmed,
                PaymentMethod = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2021, 12, 11),
                DeadlineDate = new DateTime(2021, 12, 9),
                AgentId = AgentId
            },
            new Booking
            {
                Id = 3,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-224",
                Status = BookingStatuses.Confirmed,
                PaymentMethod = PaymentTypes.CreditCard,
                CheckInDate = new DateTime(2021, 12, 10),
                DeadlineDate = new DateTime(2021, 11, 9),
                AgentId = AgentId
            },
            new Booking
            {
                Id = 4,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-224",
                Status = BookingStatuses.Cancelled,
                PaymentMethod = PaymentTypes.VirtualAccount,
                CheckInDate = new DateTime(2021, 12, 20),
                AgentId = AgentId
            },
            new Booking
            {
                Id = 5,
                PaymentStatus = BookingPaymentStatuses.Authorized,
                ReferenceCode = "NNN-224",
                Status = BookingStatuses.Rejected,
                PaymentMethod = PaymentTypes.VirtualAccount,
                CheckInDate = new DateTime(2022, 12, 20),
                AgentId = AgentId
            }
        };
    }
}