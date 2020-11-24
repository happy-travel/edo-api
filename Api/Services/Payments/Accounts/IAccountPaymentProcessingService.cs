using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public interface IAccountPaymentProcessingService
    {
        Task<Result> AddMoney(int accountId, PaymentData paymentData, UserInfo user);

        Task<Result> ChargeMoney(int accountId, ChargedMoneyData paymentData, UserInfo user);

        Task<Result> RefundMoney(int accountId, ChargedMoneyData paymentData, UserInfo user);

        Task<Result> TransferToChildAgency(int recipientAccountId, MoneyAmount amount, AgentContext agent);
    }
}