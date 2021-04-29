using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Data.Documents;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents
{
    public class AgentBookingDocumentsService : IAgentBookingDocumentsService
    {
        public AgentBookingDocumentsService(IBookingDocumentsService documentsService,
            IBookingDocumentsMailingService mailingService,
            IBookingRecordManager recordManager)
        {
            _documentsService = documentsService;
            _mailingService = mailingService;
            _recordManager = recordManager;
        }
       
        
        public Task<Result<(DocumentRegistrationInfo RegistrationInfo, BookingInvoiceData Data)>> GetActualInvoice(int bookingId, AgentContext agentContext)
        {
            return _recordManager.Get(bookingId)
                .CheckPermissions(agentContext)
                .Bind(b => _documentsService.GetActualInvoice(b));
        }


        public Task<Result<BookingVoucherData>> GenerateVoucher(int bookingId, AgentContext agent, string languageCode)
        {
            return _recordManager.Get(bookingId)
                .CheckPermissions(agent)
                .Bind(b => _documentsService.GenerateVoucher(b, languageCode));
        }


        public Task<Result> SendVoucher(int bookingId, string email, AgentContext agent, string languageCode)
        {
            return _recordManager.Get(bookingId)
                .CheckPermissions(agent)
                .Bind(b => _mailingService.SendVoucher(b, email, languageCode, new SlimAgentContext(agent.AgentId, agent.AgencyId)));
        }


        public Task<Result> SendInvoice(int bookingId, string email, AgentContext agent)
        {
            return _recordManager.Get(bookingId)
                .CheckPermissions(agent)
                .Bind(b => _mailingService.SendInvoice(b, email, false, new SlimAgentContext(agent.AgentId, agent.AgencyId)));
        }


        private readonly IBookingDocumentsService _documentsService;
        private readonly IBookingDocumentsMailingService _mailingService;
        private readonly IBookingRecordManager _recordManager;
    }
}