using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IPaymentHistoryService
    {
        Task<Result<List<PaymentHistoryData>>> GetAgentHistory(PaymentHistoryRequest paymentHistoryRequest, AgentContext agent);

        Task<Result<List<PaymentHistoryData>>> GetAgencyHistory(PaymentHistoryRequest paymentHistoryRequest, AgentContext agent );
    }
}