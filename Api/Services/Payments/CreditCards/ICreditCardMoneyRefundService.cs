using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Models.Users;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public interface ICreditCardMoneyRefundService
    {
        Task<Result<CreditCardRefundResult>> Refund(CreditCardRefundMoneyRequest request,
            CreditCardPaymentInfo paymentInfo,
            string maskedNumber,
            string referenceCode,
            ApiCaller apiCaller,
            int agentId);
    }
}