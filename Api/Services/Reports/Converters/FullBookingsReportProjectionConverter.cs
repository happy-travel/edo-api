using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports;
using HappyTravel.Formatters;

namespace HappyTravel.Edo.Api.Services.Reports.Converters
{
    public class FullBookingsReportProjectionConverter : IConverter<FullBookingsReportProjection, FullBookingsReportRow>
    {
        public FullBookingsReportRow Convert(FullBookingsReportProjection projection, Func<decimal, decimal> vatAmountFunc, Func<decimal, decimal> amountExcludedVatFunc) 
            => new()
            {
                Created = DateTimeFormatters.ToDateString(projection.Created),
                ReferenceCode = projection.ReferenceCode,
                InvoiceNumber = projection.InvoiceNumber,
                AgencyName = projection.AgencyName,
                PaymentMethod = EnumFormatters.FromDescription(projection.PaymentMethod),
                GuestName = projection.GuestName,
                AccommodationName = projection.AccommodationName,
                Rooms = string.Join("; ", projection.Rooms.Select(r => EnumFormatters.FromDescription(r.Type))),
                ArrivalDate = DateTimeFormatters.ToDateString(projection.ArrivalDate),
                DepartureDate = DateTimeFormatters.ToDateString(projection.DepartureDate),
                ConfirmationNumber = projection.ConfirmationNumber,
                RoomsConfirmationNumbers = string.Join("; ", projection.Rooms.Select(r => r.SupplierRoomReferenceCode)),
                OriginalAmount = projection.OriginalAmount,
                OriginalCurrency = projection.OriginalCurrency,
                ConvertedAmount = projection.ConvertedAmount,
                ConvertedCurrency = projection.ConvertedCurrency,
                AmountExclVat = Math.Round(amountExcludedVatFunc(projection.OriginalAmount), 2),
                VatAmount = Math.Round(vatAmountFunc(projection.OriginalAmount), 2),
                Supplier = EnumFormatters.FromDescription(projection.Supplier),
                PaymentStatus = EnumFormatters.FromDescription(projection.PaymentStatus)  
            };
    }
}