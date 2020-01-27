using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public interface IAccountPaymentService
    {
        Task<Result> ReplenishAccount(int accountId, PaymentData payment);
        Task<bool> CanPayWithAccount(CustomerInfo customerInfo);
        Task<Result<AccountBalanceInfo>> GetAccountBalance(Currencies currency);
        Task<Result<string>> CaptureMoney(Booking booking);
        Task<Result<PaymentResponse>> AuthorizeMoney(AccountPaymentRequest request, CustomerInfo customerInfo, string ipAddress);
        Task<Result> VoidMoney(Booking booking);
        Task<Result<Price>> GetPendingAmount(Booking booking);
    }
}