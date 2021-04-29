using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports
{
    public readonly struct AgencyWiseReportRow
    {
        public string Date { get; init; }
        public string ReferenceCode { get; init; }
        public string InvoiceNumber { get; init; }
        public string AgencyName { get; init; }
        public string PaymentMethod { get; init; }
        public string GuestName { get; init; }
        public string AccommodationName { get; init; }
        public string Rooms { get; init; }
        public string ArrivalDate { get; init; }
        public string DepartureDate { get; init; }
        public double LenghtOfStay { get; init; }
        public decimal OriginalAmount { get; init; }
        public Currencies OriginalCurrency { get; init; }
        public decimal ConvertedAmount { get; init; }
        public Currencies ConvertedCurrency { get; init; }
        public string ConfirmationNumber { get; init; }
        public string RoomsConfirmationNumbers { get; init; }
        public string PaymentStatus { get; init; }
    }
}