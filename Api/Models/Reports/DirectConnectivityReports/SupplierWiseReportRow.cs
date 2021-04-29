using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports
{
    public readonly struct SupplierWiseReportRow
    {
        public string ReferenceCode { get; init; }
        public string InvoiceNumber { get; init; }
        public string AccommodationName { get; init; }
        public string ConfirmationNumber { get; init; }
        public string RoomsConfirmationNumbers { get; init; }
        public string RoomTypes { get; init; }
        public string GuestName { get; init; }
        public string ArrivalDate { get; init; }
        public string DepartureDate { get; init; }
        public double LenghtOfStay { get; init; }
        public decimal AmountExclVat { get; init; }
        public decimal VatAmount { get; init; }
        public decimal OriginalAmount { get; init; }
        public Currencies OriginalCurrency { get; init; }
        public decimal ConvertedAmount { get; init; }
        public Currencies ConvertedCurrency { get; init; }
        public string Supplier { get; init; }
    }
}