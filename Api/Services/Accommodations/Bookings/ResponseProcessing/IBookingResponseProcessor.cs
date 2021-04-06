using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.ResponseProcessing
{
    public interface IBookingResponseProcessor
    {
        Task ProcessResponse(Booking bookingResponse, UserInfo user, BookingChangeEvents eventType, BookingChangeInitiators initiator);
    }
}