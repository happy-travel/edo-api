using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public interface ICounterpartyAccountService
    {
        Task<List<CounterpartyAccountInfo>> Get(int counterpartyId);

        Task<Result> Activate(int counterpartyId, int counterpartyAccountId, string reason);

        Task<Result> Deactivate(int counterpartyId, int counterpartyAccountId, string reason);

        Task<Result<CounterpartyBalanceInfo>> GetBalance(int counterpartyId, Currencies currency);

        Task<Result> AddMoney(int counterpartyAccountId, PaymentData paymentData, ApiCaller apiCaller);

        Task<Result> SubtractMoney(int counterpartyAccountId, PaymentCancellationData data, ApiCaller apiCaller);

        Task<Result> TransferToDefaultAgency(int counterpartyAccountId, MoneyAmount amount, ApiCaller apiCaller);

        Task<Result> IncreaseManually(int counterpartyAccountId, PaymentData data, ApiCaller apiCaller);

        Task<Result> DecreaseManually(int counterpartyAccountId, PaymentData data, ApiCaller apiCaller);
    }
}