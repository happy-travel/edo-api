using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingsProcessingServiceTests.Cancellation
{
    public class GettingForCancellation
    {
        [Fact]
        public void Test_data_should_contain_all_payment_states()
        {
            var bookingPaymentStatuses = Bookings
                .Select(b => b.PaymentStatus);

            Assert.True(DoesCollectionHasAllEnumValues(bookingPaymentStatuses));
        }
        
        [Fact]
        public async Task Should_not_return_paid_bookings()
        {
            var service = CreateProcessingService();

            var forCancel = await service.GetForCancellation();
            
            Assert.DoesNotContain(1, forCancel);
            Assert.DoesNotContain(2, forCancel);
            Assert.DoesNotContain(3, forCancel);
        }
        
        
        [Fact]
        public async Task Should_return_unpaid_bookings()
        {
            var service = CreateProcessingService();

            var forCancel = await service.GetForCancellation();
            
            Assert.Contains(4, forCancel);
            Assert.Contains(5, forCancel);
            Assert.Contains(6, forCancel);
            Assert.Contains(7, forCancel);
        }
        
        
        [Fact]
        public async Task Should_not_return_unconfirmed_bookings()
        {
            var service = CreateProcessingService();

            var forCancel = await service.GetForCancellation();
            
            Assert.DoesNotContain(9, forCancel);
            Assert.DoesNotContain(10, forCancel);
            Assert.DoesNotContain(11, forCancel);
        }
        
        
        [Fact]
        public async Task Should_not_return_offline_paid_bookings()
        {
            var service = CreateProcessingService();

            var forCancel = await service.GetForCancellation();
            
            Assert.DoesNotContain(9, forCancel);
            Assert.DoesNotContain(10, forCancel);
            Assert.DoesNotContain(11, forCancel);
        }


        private static BookingsProcessingService CreateProcessingService()
        {
            var context = MockEdoContextFactory.Create();
            context
                .Setup(c => c.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(Bookings));

            return new BookingsProcessingService(Mock.Of<IBookingPaymentService>(),
                Mock.Of<IBookingManagementService>(),
                Mock.Of<IBookingMailingService>(),
                context.Object);
        }

        
        private bool DoesCollectionHasAllEnumValues<TEnum>(IEnumerable<TEnum> collection)
            where TEnum : Enum
        {
            var valuesInCollection = collection
                .ToHashSet()
                .OrderBy(s => s)
                .ToList();

            var availablePaymentStatuses = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .OrderBy(s => s)
                .ToList();

            return availablePaymentStatuses.SequenceEqual(valuesInCollection);
        }

        
        private static readonly Booking[] Bookings = 
        {
            new Booking {Id = 1, PaymentStatus = BookingPaymentStatuses.Authorized, Status = BookingStatuses.Pending, PaymentMethod = PaymentMethods.BankTransfer},
            new Booking {Id = 2, PaymentStatus = BookingPaymentStatuses.Authorized, Status = BookingStatuses.Confirmed, PaymentMethod = PaymentMethods.BankTransfer },
            new Booking {Id = 3, PaymentStatus = BookingPaymentStatuses.Captured, Status = BookingStatuses.Pending, PaymentMethod = PaymentMethods.BankTransfer},
            new Booking {Id = 4, PaymentStatus = BookingPaymentStatuses.Refunded, Status = BookingStatuses.Pending, PaymentMethod = PaymentMethods.CreditCard},
            new Booking {Id = 5, PaymentStatus = BookingPaymentStatuses.Voided, Status = BookingStatuses.Confirmed, PaymentMethod = PaymentMethods.CreditCard},
            new Booking {Id = 6, PaymentStatus = BookingPaymentStatuses.Voided, Status = BookingStatuses.Pending, PaymentMethod = PaymentMethods.CreditCard},
            new Booking {Id = 7, PaymentStatus = BookingPaymentStatuses.NotPaid, Status = BookingStatuses.Confirmed, PaymentMethod = PaymentMethods.CreditCard},
            new Booking {Id = 8, PaymentStatus = BookingPaymentStatuses.NotPaid, Status = BookingStatuses.Pending, PaymentMethod = PaymentMethods.CreditCard},
            new Booking {Id = 9, PaymentStatus = BookingPaymentStatuses.Refunded, Status = BookingStatuses.Cancelled, PaymentMethod = PaymentMethods.BankTransfer},
            new Booking {Id = 10, PaymentStatus = BookingPaymentStatuses.NotPaid, Status = BookingStatuses.InternalProcessing, PaymentMethod = PaymentMethods.BankTransfer},
            new Booking {Id = 11, PaymentStatus = BookingPaymentStatuses.NotPaid, Status = BookingStatuses.WaitingForResponse, PaymentMethod = PaymentMethods.Offline},
        };
    }
}