using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public interface IBookingStatusRefreshService
    {
        Task<Result> RefreshStatus(int bookingId, ApiCaller apiCaller);

        Task<Result<BatchOperationResult>> RefreshStatuses(List<int> bookingIds, ApiCaller apiCaller);

        Task<List<int>> GetBookingsToRefresh();
    }
}