using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Markups
{
    public readonly struct PaymentMarkupDataWithValue
    {
        public PaymentMarkupDataWithValue(PaymentMarkupDataData data, decimal value)
        {
            BookingId = data.BookingId;
            AgencyAccountId = data.AgencyAccountId;
            Currency = data.TargetCurrency;
            Value = value;
        }


        public int BookingId { get; }
        public int AgencyAccountId { get; }
        public Currencies Currency { get; }
        public decimal Value { get; }
    }
}