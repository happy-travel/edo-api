using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Management;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AdministratorBookingManagementService : IAdministratorBookingManagementService
    {
        public AdministratorBookingManagementService(IBookingRecordManager recordManager,
            IBookingManagementService managementService,
            IDateTimeProvider dateTimeProvider,
            IBookingRecordsUpdater recordsUpdater)
        {
            _recordManager = recordManager;
            _managementService = managementService;
            _dateTimeProvider = dateTimeProvider;
            _recordsUpdater = recordsUpdater;
        }
        
        public Task<Result> Discard(int bookingId, Administrator admin)
        {
            return GetBooking(bookingId)
                .Bind(ProcessDiscard);

            
            Task<Result> ProcessDiscard(Booking booking) 
                => _recordsUpdater.ChangeStatus(booking, BookingStatuses.Discarded, _dateTimeProvider.UtcNow(), admin.ToUserInfo(), new BookingChangeReason 
                { 
                    Initiator = BookingChangeInitiators.Administrator,
                    Source = BookingChangeSources.Administrator,
                    Event = BookingChangeEvents.Discard,
                    Reason = "Discarded by an administrator"
                });
        }


        public Task<Result> RefreshStatus(int bookingId, Administrator admin)
        {
            return GetBooking(bookingId)
                .Bind(ProcessRefresh);


            Task<Result> ProcessRefresh(Booking booking)
                => _managementService.RefreshStatus(booking, admin.ToUserInfo(), BookingChangeEvents.Refresh, BookingChangeInitiators.Administrator);
        }


        public async Task<Result> Cancel(int bookingId, Administrator admin)
        {
            return await GetBooking(bookingId)
                .Bind(Cancel);
                
            
            Task<Result> Cancel(Booking booking) 
                => _managementService.Cancel(booking, admin.ToUserInfo(), BookingChangeEvents.Cancel, BookingChangeInitiators.Administrator);
        }


        public async Task<Result> CancelManually(int bookingId, DateTime cancellationDate, string reason, Administrator admin)
        {
            return await GetBooking(bookingId)
                .Bind(CancelManually);


            Task<Result> CancelManually(Booking booking)
                => _recordsUpdater.ChangeStatus(booking, BookingStatuses.Cancelled, cancellationDate, admin.ToUserInfo(), new BookingChangeReason 
                { 
                    Initiator = BookingChangeInitiators.Administrator,
                    Source = BookingChangeSources.Administrator,
                    Event = BookingChangeEvents.CancelManually,
                    Reason = reason
                });
        }
        
        
        public async Task<Result> RejectManually(int bookingId, string reason, Administrator admin)
        {
            return await GetBooking(bookingId)
                .Bind(RejectManually);


            Task<Result> RejectManually(Booking booking)
                => _recordsUpdater.ChangeStatus(booking, BookingStatuses.Rejected, _dateTimeProvider.UtcNow(), admin.ToUserInfo(), new BookingChangeReason 
                { 
                    Initiator = BookingChangeInitiators.Administrator,
                    Source = BookingChangeSources.Administrator,
                    Event = BookingChangeEvents.RejectManually,
                    Reason = reason
                });
        }


        public async Task<Result> ConfirmManually(int bookingId, DateTime confirmationDate, string reason, Administrator admin)
        {
            return await GetBooking(bookingId)
                .Bind(ConfirmManually);


            Task<Result> ConfirmManually(Booking booking)
                => _recordsUpdater.ChangeStatus(booking, BookingStatuses.Confirmed, confirmationDate, admin.ToUserInfo(), new BookingChangeReason 
                {
                    Initiator = BookingChangeInitiators.Administrator,
                    Source = BookingChangeSources.Administrator,
                    Event = BookingChangeEvents.ConfirmManually,
                    Reason = reason
                });
        }


        private Task<Result<Booking>> GetBooking(int id) 
            => _recordManager.Get(id);


        private readonly IBookingRecordManager _recordManager;
        private readonly IBookingManagementService _managementService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IBookingRecordsUpdater _recordsUpdater;
    }
}