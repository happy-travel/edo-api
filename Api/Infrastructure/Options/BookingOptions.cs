using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Infrastructure.Options
{
    public class BookingOptions
    {
        public List<Suppliers> DisableStatusUpdateForSuppliers { get; set; }
        public decimal CreditCardPaymentCommission { get; set; }
    }
}