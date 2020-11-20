using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingRegistrationService
    {
        Task<Result<string, ProblemDetails>> Register(AccommodationBookingRequest bookingRequest, AgentContext agentContext, string languageCode);

        Task<Result<AccommodationBookingInfo, ProblemDetails>> Finalize(string referenceCode, AgentContext agentContext, string languageCode);

        Task<Result<AccommodationBookingInfo, ProblemDetails>> BookByAccount(AccommodationBookingRequest bookingRequest,
            AgentContext agentContext, string languageCode, string clientIp);

        Task VoidMoneyAndCancelBooking(Data.Booking.Booking booking, UserInfo userInfo);

        Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> BookOnProvider(Data.Booking.Booking booking, string referenceCode,
            string languageCode, bool withBookingOnSupplier = true);


        Task<Result<EdoContracts.Accommodations.Booking, ProblemDetails>> SendReceipt(EdoContracts.Accommodations.Booking details, Data.Booking.Booking booking,
            UserInfo userInfo, string email);
    }
}