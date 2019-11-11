using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Api.Models.Payments;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IPaymentProcessingService
    {
        Task<Result> AddMoney(int accountId, PaymentData paymentData, UserInfo user);
        Task<Result> ChargeMoney(int accountId, PaymentData paymentData, UserInfo user);
        Task<Result> FreezeMoney(int accountId, FrozenMoneyData paymentData, UserInfo user);
        Task<Result> ReleaseFrozenMoney(int accountId, FrozenMoneyData paymentData, UserInfo user);
        Task<Result> UnfreezeMoney(int accountId, FrozenMoneyData paymentData, UserInfo user);
    }
}
