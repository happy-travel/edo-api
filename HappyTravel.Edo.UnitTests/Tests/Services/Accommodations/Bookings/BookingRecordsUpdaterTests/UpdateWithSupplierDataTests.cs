using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.Notifications;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Bookings.BookingRecordsUpdaterTests
{
    public class UpdateWithSupplierDataTests
    {
        public UpdateWithSupplierDataTests()
        {
            _bookedRooms = new List<BookedRoom>
            {
                MakeBookedRoom("a"),
                MakeBookedRoom("b"),
                MakeBookedRoom("c")
            };
            
            _bookings = new List<Booking>
            {
                new()
                {
                    Id = 1,
                    PaymentStatus = BookingPaymentStatuses.Authorized,
                    Status = BookingStatuses.Pending,
                    PaymentMethod = PaymentTypes.CreditCard,
                    AgentId = 1,
                    AgencyId = 1,
                    Rooms = _bookedRooms
                }
            };
            
            _updatedRoomsSameNumber = new List<SlimRoomOccupation>
            {
                MakeSlimRoomOccupation("e"),
                MakeSlimRoomOccupation("d"),
                MakeSlimRoomOccupation("f")
            };
            
            _updatedRoomsDifferentNumber = new List<SlimRoomOccupation>
            {
                MakeSlimRoomOccupation("e"),
                MakeSlimRoomOccupation("d"), 
            };

            _context = CreateContext();
            _service = CreateBookingRecordsUpdaterService();

            static BookedRoom MakeBookedRoom(string supplierRoomReferenceCode) =>
                new(default, default, default, default, default, default, default, default, default, default, default, supplierRoomReferenceCode);

            static SlimRoomOccupation MakeSlimRoomOccupation(string supplierRoomReferenceCode)
                => new(RoomTypes.Single, new List<Pax>(), supplierRoomReferenceCode); 
        }
        
        
        [Fact]
        public async Task Updating_rooms_with_same_number_of_rooms_should_change_rooms_supplier_code()
        {
            await _service.UpdateWithSupplierData(_bookings.First(), default, default, _updatedRoomsSameNumber);
        
            var rooms = _context.Bookings.First().Rooms;
            Assert.Equal(rooms[0].SupplierRoomReferenceCode, _updatedRoomsSameNumber[0].SupplierRoomReferenceCode);
            Assert.Equal(rooms[1].SupplierRoomReferenceCode, _updatedRoomsSameNumber[1].SupplierRoomReferenceCode);
            Assert.Equal(rooms[2].SupplierRoomReferenceCode, _updatedRoomsSameNumber[2].SupplierRoomReferenceCode);
        }
        
        
        [Fact]
        public async Task Updating_rooms_with_different_number_shouldnt_change_rooms_supplier_code()
        {
            await _service.UpdateWithSupplierData(_bookings.First(), default, default, _updatedRoomsDifferentNumber);
        
            var rooms = _context.Bookings.First().Rooms;
            Assert.Equal(_bookedRooms[0].SupplierRoomReferenceCode, rooms[0].SupplierRoomReferenceCode);
            Assert.Equal(_bookedRooms[1].SupplierRoomReferenceCode, rooms[1].SupplierRoomReferenceCode);
            Assert.Equal(_bookedRooms[2].SupplierRoomReferenceCode, rooms[2].SupplierRoomReferenceCode);
        }


        private EdoContext CreateContext()
        {
            var context = MockEdoContextFactory.Create();
            
            context
                .Setup(x => x.Bookings)
                .Returns(DbSetMockProvider.GetDbSetMock(_bookings));

            return context.Object;
        }
        
        
        private BookingRecordsUpdater CreateBookingRecordsUpdaterService()
        {
            return new(
                Mock.Of<IDateTimeProvider>(), 
                Mock.Of<IBookingInfoService>(), 
                Mock.Of<IBookingNotificationService>(),
                Mock.Of<IBookingMoneyReturnService>(),
                Mock.Of<IBookingDocumentsMailingService>(),
                Mock.Of<ISupplierOrderService>(),
                Mock.Of<ISendingNotificationsService>(),
                Mock.Of<IBookingChangeLogService>(),
                _context, 
                Mock.Of<ILogger<BookingRecordsUpdater>>());
        }
        
        
        private readonly EdoContext _context;
        private readonly BookingRecordsUpdater _service;
        private readonly List<BookedRoom> _bookedRooms;
        private readonly List<Booking> _bookings;
        private readonly List<SlimRoomOccupation> _updatedRoomsSameNumber;
        private readonly List<SlimRoomOccupation> _updatedRoomsDifferentNumber;
    }
}