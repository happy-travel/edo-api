using System;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Markups
{
    public readonly struct PaymentMarkupDataData : IEquatable<PaymentMarkupDataData>
    {
        public PaymentMarkupDataData(int bookingId, int agencyAccountId, Currencies sourceCurrency, Currencies targetCurrency)
        {
            BookingId = bookingId;
            AgencyAccountId = agencyAccountId;
            SourceCurrency = sourceCurrency;
            TargetCurrency = targetCurrency;

        }

        public int BookingId { get; }
        public int AgencyAccountId { get; }
        public Currencies SourceCurrency { get; }
        public Currencies TargetCurrency { get; }


        public bool Equals(PaymentMarkupDataData other)
            => BookingId == other.BookingId
                && AgencyAccountId == other.AgencyAccountId
                && SourceCurrency == other.SourceCurrency
                && TargetCurrency == other.TargetCurrency;


        public override bool Equals(object obj) => obj is PaymentMarkupDataData other && Equals(other);


        public override int GetHashCode() => HashCode.Combine(BookingId, AgencyAccountId, (int) SourceCurrency, (int) TargetCurrency);
    }
}