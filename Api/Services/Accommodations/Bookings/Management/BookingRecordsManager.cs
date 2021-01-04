using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.EntityFrameworkCore;
using Booking = HappyTravel.Edo.Data.Bookings.Booking;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    internal class BookingRecordsManager : IBookingRecordsManager
    {
        public BookingRecordsManager(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ITagProcessor tagProcessor,
            IAppliedBookingMarkupRecordsManager appliedBookingMarkupRecordsManager,
            ISupplierOrderService supplierOrderService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _tagProcessor = tagProcessor;
            _appliedBookingMarkupRecordsManager = appliedBookingMarkupRecordsManager;
            _supplierOrderService = supplierOrderService;
        }


        public async Task<string> Register(AccommodationBookingRequest bookingRequest,
            BookingAvailabilityInfo availabilityInfo, AgentContext agentContext, string languageCode)
        {
            var (_, _, referenceCode, _) = await Result.Success()
                .Map(GetTags)
                .Map(Create)
                .Map(SaveMarkups)
                .Map(CreateSupplierOrder);

            return referenceCode;


            async Task<(string itn, string referenceCode)> GetTags()
            {
                string itn;
                if (string.IsNullOrWhiteSpace(bookingRequest.ItineraryNumber))
                {
                    itn = await _tagProcessor.GenerateItn();
                }
                else
                {
                    // User can send reference code instead of itn
                    if (!_tagProcessor.TryGetItnFromReferenceCode(bookingRequest.ItineraryNumber, out itn))
                        itn = bookingRequest.ItineraryNumber;

                    if (!await AreExistBookingsForItn(itn, agentContext.AgentId))
                        itn = await _tagProcessor.GenerateItn();
                }

                var referenceCode = await _tagProcessor.GenerateReferenceCode(
                    ServiceTypes.HTL,
                    availabilityInfo.CountryCode,
                    itn);

                return (itn, referenceCode);
            }


            async Task<Booking> Create((string itn, string referenceCode) tags)
            {
                var createdBooking = BookingFactory.Create(
                    _dateTimeProvider.UtcNow(),
                    agentContext,
                    tags.itn,
                    tags.referenceCode,
                    availabilityInfo,
                    bookingRequest.PaymentMethod,
                    bookingRequest,
                    languageCode,
                    availabilityInfo.Supplier,
                    availabilityInfo.RoomContractSet.Deadline.Date,
                    availabilityInfo.CheckInDate,
                    availabilityInfo.CheckOutDate);

                _context.Bookings.Add(createdBooking);
                await _context.SaveChangesAsync();
                _context.Entry(createdBooking).State = EntityState.Detached;

                return createdBooking;
            }


            async Task<Booking> SaveMarkups(Booking booking)
            {
                await _appliedBookingMarkupRecordsManager.Create(booking.ReferenceCode, availabilityInfo.AppliedMarkups);
                return booking;
            }


            async Task<string> CreateSupplierOrder(Booking booking)
            {
                await _supplierOrderService.Add(booking.ReferenceCode, ServiceTypes.HTL, availabilityInfo.SupplierPrice, booking.Supplier);
                return booking.ReferenceCode;
            }
        }


        public async Task UpdateBookingDetails(EdoContracts.Accommodations.Booking bookingDetails, Booking booking)
        {
            booking.SupplierReferenceCode = bookingDetails.SupplierReferenceCode;
            booking.Status = bookingDetails.Status.ToInternalStatus();
            booking.UpdateMode = bookingDetails.BookingUpdateMode;
            booking.Rooms = UpdateSupplierReferenceCodes(booking.Rooms, bookingDetails.Rooms);
            
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            _context.Entry(booking).State = EntityState.Detached;


            static List<BookedRoom> UpdateSupplierReferenceCodes(List<BookedRoom> existingRooms, List<SlimRoomOccupation> updatedRooms)
            {
                // TODO: NIJO-928 Find corresponding room in more solid way
                // We cannot find corresponding room if room count differs
                if (updatedRooms == null || existingRooms.Count != updatedRooms.Count)
                    return existingRooms;
                
                var changedBookedRooms = new List<BookedRoom>(existingRooms.Count);
                for (var i = 0; i < updatedRooms.Count; i++)
                {
                    var changedBookedRoom = new BookedRoom(existingRooms[i], updatedRooms[i].SupplierRoomReferenceCode);
                    changedBookedRooms.Add(changedBookedRoom);
                }

                return changedBookedRooms;
            }
        }


        public Task Confirm(EdoContracts.Accommodations.Booking bookingDetails, Booking booking)
        {
            booking.ConfirmationDate = _dateTimeProvider.UtcNow();
            return UpdateBookingDetails(bookingDetails, booking);
        }


        public async Task SetStatus(string referenceCode, BookingStatuses status)
        {
            var (_, _, booking, _) = await Get(referenceCode);
            booking.Status = status;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }


        public Task<Result<Booking>> Get(string referenceCode)
        {
            return Get(booking => booking.ReferenceCode == referenceCode);
        }


        public Task<Result<Booking>> Get(int bookingId)
        {
            return Get(booking => booking.Id == bookingId);
        }

        
        public async Task<Result<Booking>> Get(int bookingId, int agentId, int agencyId)
        {
            var permissions = await _context.AgentAgencyRelations
                .Where(a => a.AgencyId == agencyId && a.AgentId == agentId)
                .Select(a => a.InAgencyPermissions)
                .SingleOrDefaultAsync();

            if (permissions.HasFlag(InAgencyPermissions.AgencyBookingsManagement))
                return await GetByAgency(bookingId, agencyId);

            return await GetByAgent(bookingId, agentId);
        }
        
        
        private Task<Result<Booking>> GetByAgent(int bookingId, int agentId)
        {
            return Get(booking => booking.Id == bookingId && booking.AgentId == agentId);
        }
        
        
        private Task<Result<Booking>> GetByAgency(int bookingId, int agencyId)
        {
            return Get(booking => booking.Id == bookingId && booking.AgencyId == agencyId);
        }
        

        private async Task<Result<Booking>> Get(Expression<Func<Booking, bool>> filterExpression)
        {
            var booking = await _context.Bookings
                .Where(filterExpression)
                .SingleOrDefaultAsync();

            return booking == default
                ? Result.Failure<Booking>("Could not get booking data")
                : Result.Success(booking);
        }


        // TODO: Replace method when will be added other services 
        private Task<bool> AreExistBookingsForItn(string itn, int agentId)
            => _context.Bookings.Where(b => b.AgentId == agentId && b.ItineraryNumber == itn).AnyAsync();


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITagProcessor _tagProcessor;
        private readonly IAppliedBookingMarkupRecordsManager _appliedBookingMarkupRecordsManager;
        private readonly ISupplierOrderService _supplierOrderService;
    }
}