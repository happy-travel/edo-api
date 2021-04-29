using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Reports.DirectConnectivityReports;
using HappyTravel.Formatters;

namespace HappyTravel.Edo.Api.Services.Reports.Converters
{
    public class AgencyWiseRecordProjectionConverter : IConverter<AgencyWiseRecordProjection, AgencyWiseReportRow>
    {
        public AgencyWiseReportRow Convert(AgencyWiseRecordProjection projection, Func<decimal, decimal> vatAmountFunc, Func<decimal, decimal> amountExcludedVatFunc)
            => new()
            {
                Date = DateTimeFormatters.ToDateString(projection.Date),
                ReferenceCode = projection.ReferenceCode,
                InvoiceNumber = projection.InvoiceNumber,
                AgencyName = projection.AgencyName,
                PaymentMethod = EnumFormatters.FromDescription(projection.PaymentMethod),
                GuestName = projection.GuestName,
                AccommodationName = projection.AccommodationName,
                Rooms = string.Join("; ", projection.Rooms.Select(r => EnumFormatters.FromDescription(r.Type))),
                ArrivalDate = DateTimeFormatters.ToDateString(projection.ArrivalDate),
                DepartureDate = DateTimeFormatters.ToDateString(projection.DepartureDate),
                LenghtOfStay = (projection.DepartureDate - projection.ArrivalDate).TotalDays,
                OriginalAmount = projection.OriginalAmount,
                OriginalCurrency = projection.OriginalCurrency,
                ConvertedAmount = projection.ConvertedAmount,
                ConvertedCurrency = projection.ConvertedCurrency,
                ConfirmationNumber = projection.ConfirmationNumber,
                RoomsConfirmationNumbers = string.Join("; ", projection.Rooms.Select(r => r.SupplierRoomReferenceCode)),
                PaymentStatus = EnumFormatters.FromDescription(projection.PaymentStatus)
            };
    }
}