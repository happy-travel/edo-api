using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Models.Mailing;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public interface IBookingDocumentsService
    {
        Task<Result<BookingVoucherData>> GenerateVoucher(int bookingId, RequestMetadata requestMetadata);

        Task<Result<BookingInvoiceData>> GenerateInvoice(int bookingId, string languageCode);
    }
}