using System.Threading.Tasks;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingResponseProcessor
    {
        Task ProcessResponse(Booking bookingResponse, Data.Bookings.Booking booking);
    }
}