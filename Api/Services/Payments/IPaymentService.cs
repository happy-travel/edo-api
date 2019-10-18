using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json.Linq;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IPaymentService
    {
        IReadOnlyCollection<Currencies> GetCurrencies();
        IReadOnlyCollection<PaymentMethods> GetAvailableCustomerPaymentMethods();
        Task<Result> ReplenishAccount(int accountId, PaymentData payment);
        Task<Result<PaymentResponse>> Pay(PaymentRequest request, string languageCode, string ipAddress, CustomerInfo customerInfo);
        Task<Result<PaymentResponse>> ProcessPaymentResponse(JObject response);
        Task<bool> CanPayWithAccount(CustomerInfo customerInfo);
    }
}
