using System;
using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports
{
    public readonly struct SupplierWiseRecordProjection
    {
        public string ReferenceCode { get; init; }
        public string InvoiceNumber { get; init; }
        public string AccommodationName { get; init; }
        public string ConfirmationNumber { get; init; }
        public List<BookedRoom> Rooms { get; init; }
        public string GuestName { get; init; }
        public DateTime ArrivalDate { get; init; }
        public DateTime DepartureDate { get; init; }
        public decimal OriginalAmount { get; init; }
        public Currencies OriginalCurrency { get; init; }
        public decimal ConvertedAmount { get; init; }
        public Currencies ConvertedCurrency { get; init; }
        public Suppliers Supplier { get; init; }
    }
}