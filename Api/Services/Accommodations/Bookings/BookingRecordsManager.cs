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
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Booking = HappyTravel.Edo.Data.Bookings.Booking;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    internal class BookingRecordsManager : IBookingRecordsManager
    {
        public BookingRecordsManager(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ITagProcessor tagProcessor,
            IAccommodationService accommodationService,
            IAccommodationBookingSettingsService accommodationBookingSettingsService,
            IAppliedBookingMarkupRecordsManager appliedBookingMarkupRecordsManager,
            ISupplierOrderService supplierOrderService)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _tagProcessor = tagProcessor;
            _accommodationService = accommodationService;
            _accommodationBookingSettingsService = accommodationBookingSettingsService;
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


        public IQueryable<AgentBoundedData<SlimAccommodationBookingInfo>> GetAgencyBookingsInfo(AgentContext agentContext)
        {
            return from booking in _context.Bookings
                join agent in _context.Agents on booking.AgentId equals agent.Id
                where booking.AgencyId == agentContext.AgencyId
                select new AgentBoundedData<SlimAccommodationBookingInfo>
                {
                    Agent = new SlimAgentDescription
                    {
                        Id = agent.Id,
                        FirstName = agent.FirstName,
                        LastName = agent.LastName,
                        Position = agent.Position
                    },
                    Data = new SlimAccommodationBookingInfo
                    {
                        Id = booking.Id,
                        ReferenceCode = booking.ReferenceCode,
                        AccommodationName = booking.AccommodationName,
                        CountryName = booking.Location.Country,
                        LocalityName = booking.Location.Locality,
                        Deadline = booking.DeadlineDate,
                        Price = new MoneyAmount(booking.TotalPrice, booking.Currency),
                        CheckInDate = booking.CheckInDate,
                        CheckOutDate = booking.CheckOutDate,
                        Status = booking.Status,
                        PaymentStatus = booking.PaymentStatus,
                        Rooms = booking.Rooms,
                        Supplier = booking.Supplier
                    }
                };
        }


        public Task Confirm(EdoContracts.Accommodations.Booking bookingDetails, Booking booking)
        {
            booking.ConfirmationDate = _dateTimeProvider.UtcNow();
            return UpdateBookingDetails(bookingDetails, booking);
        }


        public async Task SetStatus(Booking booking, BookingStatuses status)
        {
            booking.Status = status;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }


        public async Task SetPaymentStatus(Booking booking, BookingPaymentStatuses paymentStatus)
        {
            booking.PaymentStatus = paymentStatus;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            _context.Entry(booking).State = EntityState.Detached;
        }


        public Task<Result<Booking>> Get(string referenceCode)
        {
            return Get(booking => booking.ReferenceCode == referenceCode);
        }


        public Task<Result<Booking>> Get(int bookingId)
        {
            return Get(booking => booking.Id == bookingId);
        }

        
        public Task<Result<Booking>> Get(int bookingId, int agentId)
        {
            return Get(booking => booking.Id == bookingId && booking.AgentId == agentId);
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


        public async Task<Result<Booking>> GetAgentsBooking(string referenceCode, AgentContext agentContext)
        {
            return await Get(booking => agentContext.AgentId == booking.AgentId && booking.ReferenceCode == referenceCode);
        }


        public async Task<Result<AccommodationBookingInfo>> GetAgentAccommodationBookingInfo(int bookingId, AgentContext agentContext, string languageCode)
        {
            var bookingDataResult = await Get(booking => booking.Id == bookingId);
            if (bookingDataResult.IsFailure)
                return Result.Failure<AccommodationBookingInfo>(bookingDataResult.Error);

            if (!BookingPermissionHelper.DoesAgentHavePermissions(bookingDataResult.Value, agentContext))
                return Result.Failure<AccommodationBookingInfo>("Permission denied");

            var (_, isFailure, bookingInfo, error) = await ConvertToBookingInfo(bookingDataResult.Value, languageCode, agentContext);
            if (isFailure)
                return Result.Failure<AccommodationBookingInfo>(error);

            return bookingInfo;
        }


        public async Task<Result<AccommodationBookingInfo>> GetAgentAccommodationBookingInfo(string referenceCode, AgentContext agentContext, string languageCode)
        {
            var bookingDataResult = await Get(booking => agentContext.AgentId == booking.AgentId && booking.ReferenceCode == referenceCode);
            if (bookingDataResult.IsFailure)
                return Result.Failure<AccommodationBookingInfo>(bookingDataResult.Error);
            
            if (!BookingPermissionHelper.DoesAgentHavePermissions(bookingDataResult.Value, agentContext))
                return Result.Failure<AccommodationBookingInfo>("Permission denied");

            var (_, isFailure, bookingInfo, error) = await ConvertToBookingInfo(bookingDataResult.Value, languageCode, agentContext);
            if (isFailure)
                return Result.Failure<AccommodationBookingInfo>(error);

            return bookingInfo;
        }

        
        public async Task<Result<AccommodationBookingInfo>> GetAccommodationBookingInfo(string referenceCode, string languageCode)
        {
            var bookingDataResult = await Get(booking => booking.ReferenceCode == referenceCode);
            if (bookingDataResult.IsFailure)
                return Result.Failure<AccommodationBookingInfo>(bookingDataResult.Error);
            
            var (_, isFailure, bookingInfo, error) = await ConvertToBookingInfo(bookingDataResult.Value, languageCode);
            if (isFailure)
                return Result.Failure<AccommodationBookingInfo>(error);

            return bookingInfo;
        }

        /// <summary>
        /// Gets all booking info of the current agent
        /// </summary>
        /// <returns>List of the slim booking models </returns>
        public IQueryable<SlimAccommodationBookingInfo> GetAgentBookingsInfo(AgentContext agentContext)
        {
            var bookingData = _context.Bookings
                .Where(b => b.AgentId == agentContext.AgentId)
                .Where(b => 
                    (b.PaymentMethod == PaymentMethods.BankTransfer)
                    || (b.PaymentMethod != PaymentMethods.BankTransfer && b.PaymentStatus != BookingPaymentStatuses.NotPaid))
                .Select(b =>
                    new SlimAccommodationBookingInfo
                    {
                        Id = b.Id,
                        ReferenceCode = b.ReferenceCode,
                        AccommodationName = b.AccommodationName,
                        CountryName = b.Location.Country,
                        LocalityName = b.Location.Locality,
                        Deadline = b.DeadlineDate,
                        Price = new MoneyAmount(b.TotalPrice, b.Currency),
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        Status = b.Status,
                        PaymentStatus = b.PaymentStatus,
                        Rooms = b.Rooms,
                        Supplier = b.Supplier
                    }
                );

            return bookingData;
        }


        public async Task<Result> SetPaymentMethod(string referenceCode, PaymentMethods paymentMethod)
        {
            return await Get(referenceCode)
                .Tap(SetPaymentMethod);


            async Task SetPaymentMethod(Booking booking)
            {
                if (booking.PaymentMethod == paymentMethod)
                    return;

                booking.PaymentMethod = paymentMethod;

                _context.Update(booking);
                await _context.SaveChangesAsync();

                _context.Entry(booking).State = EntityState.Detached;
            }
        }

        
        private async Task<Result<AccommodationBookingInfo>> ConvertToBookingInfo(Booking booking, string languageCode, AgentContext? agentContext = null)
        {
            var (_, isFailure, accommodation, error) = await _accommodationService.Get(booking.Supplier, booking.AccommodationId, languageCode);
            if (isFailure)
                return Result.Failure<AccommodationBookingInfo>(error.Detail);
            
            var bookingDetails = GetDetails(booking, accommodation);
            var supplier = await GetSupplier(booking, agentContext);
            var agentInformation = await GetAgentInformation(booking.AgentId, booking.AgencyId);

            return new AccommodationBookingInfo(booking.Id,
                bookingDetails,
                booking.CounterpartyId,
                booking.PaymentStatus,
                new MoneyAmount(booking.TotalPrice, booking.Currency),
                supplier,
                agentInformation);


            static AccommodationBookingDetails GetDetails(Booking booking, Accommodation accommodationDetails)
            {
                var passengerNumber = booking.Rooms.Sum(r => r.Passengers.Count);
                var numberOfNights = (booking.CheckOutDate - booking.CheckInDate).Days;
                return new AccommodationBookingDetails(booking.ReferenceCode,
                    booking.SupplierReferenceCode,
                    booking.Status,
                    numberOfNights,
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    booking.Location,
                    accommodationDetails.Contacts,
                    booking.AccommodationId,
                    booking.AccommodationName,
                    booking.DeadlineDate,
                    booking.Rooms,
                    passengerNumber);
            }
            
            
            async Task<Suppliers?> GetSupplier(Booking booking, AgentContext? agent)
            {
                if (agent == null)
                    return booking.Supplier;
            
                var settings = await _accommodationBookingSettingsService.Get(agent.Value);
                return settings.IsSupplierVisible
                    ? booking.Supplier
                    : (Suppliers?) null;
            }
            
            
            Task<AccommodationBookingInfo.BookingAgentInformation> GetAgentInformation(int agentId, int agencyId)
            {
                var agencyInfoQuery = from agent in _context.Agents
                    join relation in _context.AgentAgencyRelations on agent.Id equals relation.AgentId
                    join agency in _context.Agencies on relation.AgencyId equals agency.Id
                    join counterparty in _context.Counterparties on agency.CounterpartyId equals counterparty.Id
                    where agent.Id == booking.AgentId && agency.Id == booking.AgencyId
                    let agentName = $"{agent.FirstName} {agent.LastName}"
                    select new AccommodationBookingInfo.BookingAgentInformation(agentName,
                        agency.Name, counterparty.Name, agent.Email);

                return agencyInfoQuery.SingleOrDefaultAsync();
            }
        }


        // TODO: Replace method when will be added other services 
        private Task<bool> AreExistBookingsForItn(string itn, int agentId)
            => _context.Bookings.Where(b => b.AgentId == agentId && b.ItineraryNumber == itn).AnyAsync();


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITagProcessor _tagProcessor;
        private readonly IAccommodationService _accommodationService;
        private readonly IAccommodationBookingSettingsService _accommodationBookingSettingsService;
        private readonly IAppliedBookingMarkupRecordsManager _appliedBookingMarkupRecordsManager;
        private readonly ISupplierOrderService _supplierOrderService;
    }
}