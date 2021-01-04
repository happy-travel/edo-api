using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management
{
    public interface IBookingRecordsManager
    {
        Task<Result<Booking>> Get(string referenceCode);
        
        Task<Result<Booking>> Get(int id);

        Task<Result<Booking>> GetByAgent(int bookingId, int agentId);

        Task<Result<Booking>> GetByAgency(int bookingId, int agencyId);

        Task Confirm(EdoContracts.Accommodations.Booking bookingDetails, Booking booking);
        
        Task UpdateBookingDetails(EdoContracts.Accommodations.Booking bookingDetails, Booking booking);
 
        Task<string> Register(AccommodationBookingRequest bookingRequest, BookingAvailabilityInfo bookingAvailability, AgentContext agentContext, string languageCode);

        Task SetStatus(string referenceCode, BookingStatuses status);
    }
}