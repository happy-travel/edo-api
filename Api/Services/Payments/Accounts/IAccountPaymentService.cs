using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public interface IAccountPaymentService
    {
        Task<bool> CanPayWithAccount(AgentContext agentContext);
        Task<List<AgencyAccountInfo>> GetAgencyAccounts(AgentContext agentContext);
        Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency, AgentContext agent);
        Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency, int agencyId);
        Task<Result<PaymentResponse>> Charge(string referenceCode, ApiCaller apiCaller, IPaymentCallbackService paymentCallbackService);
        Task<Result> Refund(string referenceCode, ApiCaller apiCaller, DateTime operationDate, IPaymentCallbackService paymentCallbackService, string reason);
        Task<Result> TransferToChildAgency(int payerAccountId, int recipientAccountId, MoneyAmount amount, AgentContext agent);
    }
}