using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.BatchProcessing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;
using HappyTravel.Edo.UnitTests.Utility;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingsProcessingServiceTests.Cancellation
{
    public class ExecutingCancellation
    {
        [Fact]
        public async Task Cancel_valid_bookings_should_succeed()
        {
            var service = CreateProcessingService(Mock.Of<ISupplierBookingManagementService>());

            var (isSuccess, _, _, _) = await service.Cancel(new List<int> {1, 2}, new ServiceAccount() {ClientId = "ClientId", Id = 12});

            Assert.True(isSuccess);
        }
        
        
        [Fact]
        public async Task Cancel_invalid_booking_should_fail()
        {
            var service = CreateProcessingService(Mock.Of<ISupplierBookingManagementService>());

            var (_, isFailure, _, _) = await service.Cancel(new List<int> {3, 4}, new ServiceAccount() {ClientId = "ClientId", Id = 12});

            Assert.True(isFailure);
        }
        
        
        [Fact]
        public async Task All_bookings_should_be_cancelled()
        {
            var bookingServiceMock = new Mock<ISupplierBookingManagementService>();
            var service = CreateProcessingService(bookingServiceMock.Object);

            await service.Cancel(new List<int> {1, 2}, new ServiceAccount() {ClientId = "ClientId", Id = 12});

            bookingServiceMock
                .Verify(
                    b => b.Cancel(It.IsAny<Booking>(), It.IsAny<ApiCaller>(), It.IsAny<BookingChangeEvents>()),
                    Times.Exactly(2)
                );
        }


        private BookingsProcessingService CreateProcessingService(ISupplierBookingManagementService supplierBookingManagementService)
        {
            var context = MockEdoContextFactory.Create();
            context.Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(Bookings));

            var service = new BookingsProcessingService(Mock.Of<IBookingAccountPaymentService>(),
                Mock.Of<IBookingCreditCardPaymentService>(), 
               supplierBookingManagementService, 
               Mock.Of<IBookingNotificationService>(),
                Mock.Of<IBookingReportsService>(),
                context.Object,
                Mock.Of<IBookingRecordsUpdater>(),
                Mock.Of<IDateTimeProvider>());
            return service;
        }


        private static readonly Booking[] Bookings =
        {
            new Booking
            {
                Id = 1, PaymentStatus = BookingPaymentStatuses.NotPaid, ReferenceCode = "NNN-222", Status = BookingStatuses.Confirmed, PaymentType = PaymentTypes.CreditCard
            },
            new Booking
            {
                Id = 2, PaymentStatus = BookingPaymentStatuses.Refunded, ReferenceCode = "NNN-223", Status = BookingStatuses.Confirmed, PaymentType = PaymentTypes.CreditCard
            },
            new Booking
            {
                Id = 3, PaymentStatus = BookingPaymentStatuses.Authorized, ReferenceCode = "NNN-224", Status = BookingStatuses.Confirmed, PaymentType = PaymentTypes.CreditCard
            }
        };
    }
}