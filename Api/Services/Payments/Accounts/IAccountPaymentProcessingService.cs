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
        Task<Result> ChargeMoney(int accountId, ChargedMoneyData paymentData, ApiCaller apiCaller);

        Task<Result> RefundMoney(int accountId, ChargedMoneyData paymentData, ApiCaller apiCaller);

        Task<Result> TransferToChildAgency(int payerAccountId, int recipientAccountId, MoneyAmount amount, AgentContext agent);
    }
}