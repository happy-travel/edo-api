using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public interface ICreditCardMoneyCaptureService
    {
        Task<Result<CreditCardCaptureResult>> Capture(CreditCardCaptureMoneyRequest request,
            CreditCardPaymentInfo paymentInfo,
            string maskedNumber,
            Currencies currency,
            ApiCaller apiCaller,
            int agentId);


        Task<Result<CreditCardVoidResult>> Void(CreditCardVoidMoneyRequest request,
            CreditCardPaymentInfo paymentInfo,
            string maskedNumber,
            MoneyAmount moneyAmount,
            string referenceCode,
            ApiCaller apiCaller,
            int agentId);
    }
}