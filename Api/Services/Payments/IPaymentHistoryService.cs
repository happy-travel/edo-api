using System.Linq;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IPaymentHistoryService
    {
        IQueryable<PaymentHistoryData> GetAgentHistory(AgentContext agent);

        IQueryable<PaymentHistoryData> GetAgencyHistory(AgentContext agent);
    }
}