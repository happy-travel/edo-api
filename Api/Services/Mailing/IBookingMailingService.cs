using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;

namespace HappyTravel.Edo.Api.Services.Mailing
{
    public interface IBookingMailingService
    {
        Task<Result> SendVoucher(int bookingId, string email, AgentContext agent, string languageCode);

        Task<Result> SendInvoice(int bookingId, string email, AgentContext agent, string languageCode);

        Task NotifyBookingCancelled(AccommodationBookingInfo bookingInfo);

        Task NotifyBookingFinalized(AccommodationBookingInfo bookingInfo);

        Task<Result> NotifyDeadlineApproaching(int bookingId, string email);

        Task<Result<string>> SendBookingReports(int agencyId);

        Task<Result> SendBookingsAdministratorSummary();
        
        Task<Result> SendBookingsPaymentsSummaryToAdministrator();

        Task NotifyCreditCardPaymentConfirmation(string referenceCode);
    }
}