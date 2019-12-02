using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments.Payfort;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IPayfortService
    {
        Task<Result<CreditCardPaymentResult>> Authorize(CreditCardPaymentRequest request);

        Task<Result<CreditCardPaymentResult>> Pay(CreditCardPaymentRequest request);

        Result<CreditCardPaymentResult> ProcessPaymentResponse(JObject response);

        Task<Result> Capture(CreditCardCaptureMoneyRequest moneyRequest);

        Task<Result> Void(CreditCardVoidMoneyRequest moneyRequest);
    }
}