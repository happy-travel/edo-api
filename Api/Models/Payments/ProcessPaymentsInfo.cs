using System;

namespace HappyTravel.Edo.Api.Models.Payments
{
    public readonly struct ProcessPaymentsInfo
    {
        public ProcessPaymentsInfo(DateTime date)
        {
            Date = date;
        }

        public DateTime Date { get; }
    }
}