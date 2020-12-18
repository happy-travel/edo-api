using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.EdoContracts.General.Enums;


namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingRecordsManager
    {
        Task<Result<AccommodationBookingInfo>> GetAgentAccommodationBookingInfo(int bookingId, AgentContext agentContext, string languageCode);
        
        Task<Result<AccommodationBookingInfo>> GetAccommodationBookingInfo(string referenceCode, string languageCode);
        
        Task<Result<AccommodationBookingInfo>> GetAgentAccommodationBookingInfo(string referenceCode, AgentContext agentContext, string languageCode);
        
        Task<Result<Booking>> Get(string referenceCode);
        
        Task<Result<Booking>> Get(int id);

        Task<Result<Booking>> Get(int bookingId, int agentId);
        
        IQueryable<SlimAccommodationBookingInfo> GetAgentBookingsInfo(AgentContext agentContext);
        
        IQueryable<AgentBoundedData<SlimAccommodationBookingInfo>> GetAgencyBookingsInfo(AgentContext agentContext);
        
        Task Confirm(EdoContracts.Accommodations.Booking bookingDetails, Booking booking);
        
        Task UpdateBookingDetails(EdoContracts.Accommodations.Booking bookingDetails, Booking booking);
 
        Task<string> Register(AccommodationBookingRequest bookingRequest, BookingAvailabilityInfo bookingAvailability, AgentContext agentContext, string languageCode);

        Task<Result<Booking>> GetAgentsBooking(string referenceCode, AgentContext agentContext);

        Task<Result> SetPaymentMethod(string referenceCode, PaymentMethods paymentMethod);

        Task SetStatus(Booking booking, BookingStatuses status);

        Task SetPaymentStatus(Booking booking, BookingPaymentStatuses paymentStatus);
    }
}