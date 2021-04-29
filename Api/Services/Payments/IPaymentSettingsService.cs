using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IPaymentSettingsService
    {
        IReadOnlyCollection<Currencies> GetCurrencies();

        IReadOnlyCollection<PaymentTypes> GetAvailableAgentPaymentTypes();
    }
}