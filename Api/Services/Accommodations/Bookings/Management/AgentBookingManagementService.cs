using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public class AgentBookingManagementService : IAgentBookingManagementService
    {
        public AgentBookingManagementService(IBookingManagementService managementService, 
            IBookingRecordManager recordManager, IBookingStatusRefreshService statusRefreshService)
        {
            _managementService = managementService;
            _recordManager = recordManager;
            _statusRefreshService = statusRefreshService;
        }


        public async Task<Result> Cancel(int bookingId, AgentContext agent)
        {
            return await GetBooking(bookingId, agent)
                .Bind(booking => CheckIsUsingAgencyOfBooking(booking, agent))
                .Bind(Cancel);

            
            Task<Result> Cancel(Booking booking) 
                => _managementService.Cancel(booking, agent.ToUserInfo());
        }
        
        
        public async Task<Result> Cancel(string referenceCode, AgentContext agent)
        {
            return await GetBooking(referenceCode, agent)
                .Bind(booking => CheckIsUsingAgencyOfBooking(booking, agent))
                .Bind(Cancel);

            
            Task<Result> Cancel(Booking booking) 
                => _managementService.Cancel(booking, agent.ToUserInfo());
        }

        
        public async Task<Result> RefreshStatus(int bookingId, AgentContext agent)
        {
            return await GetBooking(bookingId, agent)
                .Bind(booking => CheckIsUsingAgencyOfBooking(booking, agent))
                .Bind(Refresh);

            
            Task<Result> Refresh(Booking booking) 
                => _statusRefreshService.RefreshStatus(booking.Id, agent.ToUserInfo());
        }


        private Task<Result<Booking>> GetBooking(int bookingId, AgentContext agent) 
            => _recordManager.Get(bookingId, agent.AgentId);
        
        
        private Task<Result<Booking>> GetBooking(string referenceCode, AgentContext agent) 
            => _recordManager.Get(referenceCode, agent.AgentId);


        private Result<Booking> CheckIsUsingAgencyOfBooking(Booking booking, AgentContext agent)
            => agent.AgencyId == booking.AgencyId
                ? booking
                : Result.Failure<Booking>("This booking was created using different agency");

        
        private readonly IBookingManagementService _managementService;
        private readonly IBookingRecordManager _recordManager;
        private readonly IBookingStatusRefreshService _statusRefreshService;
    }
}