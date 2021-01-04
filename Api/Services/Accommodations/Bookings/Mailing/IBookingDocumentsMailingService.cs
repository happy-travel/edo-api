using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Data.Documents;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing
{
    public interface IBookingDocumentsMailingService
    {
        Task<Result> SendVoucher(int bookingId, string email, AgentContext agent, string languageCode);

        Task<Result> SendInvoice(int bookingId, string email, int agentId, int agencyId);
        
        Task<Result> SendReceiptToCustomer((DocumentRegistrationInfo RegistrationInfo, PaymentReceipt Data) receipt, string email);
    }
}