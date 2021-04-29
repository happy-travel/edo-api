using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public class PaymentSettingsService : IPaymentSettingsService
    {
        public IReadOnlyCollection<Currencies> GetCurrencies() 
            => new ReadOnlyCollection<Currencies>(Currencies);

        public IReadOnlyCollection<PaymentTypes> GetAvailableAgentPaymentTypes() 
            => new ReadOnlyCollection<PaymentTypes>(AvailablePaymentMethods);

        private static readonly Currencies[] Currencies = Enum.GetValues(typeof(Currencies))
            .Cast<Currencies>()
            .ToArray();

        private static readonly PaymentTypes[] AvailablePaymentMethods = {PaymentTypes.VirtualAccount, PaymentTypes.CreditCard};
    }
}