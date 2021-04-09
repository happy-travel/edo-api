using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.BookingExecution.Flows
{
    public interface IOfflinePaymentBookingFlow
    {
        Task<Result<AccommodationBookingInfo>> Book(AccommodationBookingRequest bookingRequest,
            AgentContext agentContext, string languageCode, string clientIp);
    }
}