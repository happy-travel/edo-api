using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Logging;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.NotificationCenter.Models;
using HappyTravel.Edo.Api.NotificationCenter.Services;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Payments;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.DataFormatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public class BookingRecordsUpdater : IBookingRecordsUpdater
    {
        public BookingRecordsUpdater(IDateTimeProvider dateTimeProvider, IBookingInfoService infoService,
            IBookingNotificationService bookingNotificationService, IBookingMoneyReturnService moneyReturnService,
            IBookingDocumentsMailingService documentsMailingService, ISupplierOrderService supplierOrderService,
            INotificationService notificationService, IBookingChangeLogService bookingChangeLogService,
            EdoContext context, ILogger<BookingRecordsUpdater> logger)
        {
            _dateTimeProvider = dateTimeProvider;
            _infoService = infoService;
            _bookingNotificationService = bookingNotificationService;
            _moneyReturnService = moneyReturnService;
            _documentsMailingService = documentsMailingService;
            _supplierOrderService = supplierOrderService;
            _notificationsService = notificationService;
            _context = context;
            _logger = logger;
            _bookingChangeLogService = bookingChangeLogService;
        }
        

        public async Task<Result> ChangeStatus(Booking booking, BookingStatuses status, DateTime date, ApiCaller apiCaller, BookingChangeReason reason) 
        {
            if (booking.Status == status)
                return Result.Success();

            await SetStatus(booking, status);

            var message = new BookingStatusChangeInfo
            {
                BookingId = booking.Id,
                ReferenceCode = booking.ReferenceCode,
                Status = status,
                ChangeTime = _dateTimeProvider.UtcNow(),
                AccommodationName = booking.AccommodationName,
                AccommodationPhoto = booking.AccommodationInfo?.Photo,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate
            };
            await _notificationsService.Send(apiCaller, 
                JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(message, new(JsonSerializerDefaults.Web))), 
                Notifications.Enums.NotificationTypes.BookingStatusChanged);

            await _bookingChangeLogService.Write(booking, status, date, apiCaller, reason);
            
            return status switch
            {
                BookingStatuses.Confirmed => await ProcessConfirmation(booking, date),
                BookingStatuses.Cancelled => await ProcessCancellation(booking, date, apiCaller),
                BookingStatuses.Rejected => await ProcessDiscarding(booking, apiCaller),
                BookingStatuses.Invalid => await ProcessDiscarding(booking, apiCaller),
                BookingStatuses.Discarded => await ProcessDiscarding(booking, apiCaller),
                BookingStatuses.ManualCorrectionNeeded => await ProcessManualCorrectionNeeding(booking, apiCaller),
                BookingStatuses.PendingCancellation => Result.Success(),
                BookingStatuses.WaitingForResponse => Result.Success(),
                BookingStatuses.Pending => Result.Success(),
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid status value")
            };
        }


        public async Task UpdateWithSupplierData(Booking booking, string supplierReferenceCode, BookingUpdateModes updateModes,
            List<SlimRoomOccupation> updatedRooms)
        {
            booking.SupplierReferenceCode = supplierReferenceCode;
            booking.UpdateMode = updateModes;
            booking.Rooms = UpdateSupplierReferenceCodes(booking.Rooms, updatedRooms);
            
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            _context.Detach(booking);


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
        

        private async Task<Result> ProcessConfirmation(Edo.Data.Bookings.Booking booking, DateTime confirmationDate)
        {
            return await GetBookingInfo(booking.ReferenceCode, booking.LanguageCode)
                .Tap(SetConfirmationDate)
                .Tap(NotifyBookingFinalization)
                .Bind(SendInvoice)
                .OnFailure(WriteFailureLog);
            
            
            Task<Result<AccommodationBookingInfo>> GetBookingInfo(string referenceCode, string languageCode) 
                => _infoService.GetAccommodationBookingInfo(referenceCode, languageCode);


            Task SetConfirmationDate(AccommodationBookingInfo _) 
                => this.SetConfirmationDate(booking, confirmationDate);


            Task NotifyBookingFinalization(AccommodationBookingInfo bookingInfo) 
                => _bookingNotificationService.NotifyBookingFinalized(bookingInfo, new SlimAgentContext(booking.AgentId, booking.AgencyId));


            async Task<Result> SendInvoice(AccommodationBookingInfo bookingInfo)
            {
                // Booking was updated so we need to get it again
                var updatedBooking = await _context.Bookings.FindAsync(bookingInfo.BookingId);
                return await _documentsMailingService.SendInvoice(updatedBooking, bookingInfo.AgentInformation.AgentEmail, true, new SlimAgentContext(booking.AgentId, booking.AgencyId));
            }


            void WriteFailureLog(string error) 
                => _logger.LogBookingConfirmationFailure(booking.ReferenceCode, error);
        }
        
        
        private Task<Result> ProcessCancellation(Booking booking, DateTime cancellationDate, ApiCaller user)
        {
            return SendNotifications()
                .Tap(CancelSupplierOrder)
                .Bind(() => ReturnMoney(booking, cancellationDate, user));

            
            Task CancelSupplierOrder() 
                => _supplierOrderService.Cancel(booking.ReferenceCode);


            async Task<Result> SendNotifications()
            {
                var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == booking.AgentId);
                if (agent == default)
                {
                    _logger.LogWarning("Booking cancellation notification: could not find agent with id '{0}' for the booking '{1}'",
                        booking.AgentId, booking.ReferenceCode);

                    return Result.Success();
                }

                var (_, _, bookingInfo, _) = await _infoService.GetAccommodationBookingInfo(booking.ReferenceCode, booking.LanguageCode);
                await _bookingNotificationService.NotifyBookingCancelled(bookingInfo, new SlimAgentContext(booking.AgentId, booking.AgencyId));
                
                return Result.Success();
            }
        }


        private Task<Result> ProcessDiscarding(Booking booking, ApiCaller user)
        {
            return DiscardSupplierOrder()
                .Bind(() => ReturnMoney(booking, _dateTimeProvider.UtcNow(), user));
            
            
            async Task<Result> DiscardSupplierOrder()
            {
                await _supplierOrderService.Discard(booking.ReferenceCode);
                return Result.Success();
            }
        }


        private async Task<Result> ProcessManualCorrectionNeeding(Booking booking, ApiCaller user)
        {
            var additionalInfo = await 
                (from bookings in _context.Bookings
                    join agencies in _context.Agencies on bookings.AgencyId equals agencies.Id
                    join agents in _context.Agents on bookings.AgentId equals agents.Id
                    where bookings.Id == booking.Id
                    select new {AgentName = $"{agents.FirstName} {agents.LastName}", AgencyName = agencies.Name})
                .SingleOrDefaultAsync();
            
            if (additionalInfo is null)
                return Result.Failure($"Cannot get additional info for booking id '{booking.Id}'");
            
            await _bookingNotificationService.NotifyBookingManualCorrectionNeeded(
                booking.ReferenceCode,
                additionalInfo.AgentName,
                additionalInfo.AgencyName,
                DateTimeFormatters.ToDateString(booking.DeadlineDate ?? booking.CheckOutDate));
            return Result.Success();
        }

        
        private Task<Result> ReturnMoney(Booking booking, DateTime operationDate, ApiCaller user) 
            => _moneyReturnService.ReturnMoney(booking, operationDate, user);
        
        
        private async Task SetConfirmationDate(Booking booking, DateTime date)
        {
            booking.ConfirmationDate = date;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            _context.Detach(booking);;
        }


        private async Task SetStatus(Booking booking, BookingStatuses status)
        {
            booking.Status = status;
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            _context.Detach(booking);
        }


        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingInfoService _infoService;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly IBookingMoneyReturnService _moneyReturnService;
        private readonly IBookingDocumentsMailingService _documentsMailingService;
        private readonly ISupplierOrderService _supplierOrderService;
        private readonly INotificationService _notificationsService;
        private readonly EdoContext _context;
        private readonly ILogger<BookingRecordsUpdater> _logger;
        private readonly IBookingChangeLogService _bookingChangeLogService;
    }
}