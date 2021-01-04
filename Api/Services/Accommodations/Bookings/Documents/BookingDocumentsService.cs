using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Documents;
using HappyTravel.Edo.Api.Services.Files;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Bookings;
using HappyTravel.Edo.Data.Documents;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Formatters;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Booking = HappyTravel.Edo.Data.Bookings.Booking;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents
{
    public class BookingDocumentsService : IBookingDocumentsService
    {
        public BookingDocumentsService(EdoContext context,
            IOptions<BankDetails> bankDetails, 
            IBookingRecordsManager bookingRecordsManager, 
            IAccommodationService accommodationService,
            ICounterpartyService counterpartyService,
            IInvoiceService invoiceService,
            IReceiptService receiptService,
            IImageFileService imageFileService)
        {
            _context = context;
            _bankDetails = bankDetails.Value;
            _bookingRecordsManager = bookingRecordsManager;
            _accommodationService = accommodationService;
            _counterpartyService = counterpartyService;
            _invoiceService = invoiceService;
            _receiptService = receiptService;
            _imageFileService = imageFileService;
        }


        public async Task<Result<BookingVoucherData>> GenerateVoucher(int bookingId, AgentContext agent, string languageCode)
        {
            var (_, isBookingFailure, booking, bookingError) = await _bookingRecordsManager.Get(bookingId);
            if (isBookingFailure)
                return Result.Failure<BookingVoucherData>(bookingError);

            var (_, isAccommodationFailure, accommodationDetails, accommodationError) = await _accommodationService.Get(booking.Supplier, 
                booking.AccommodationId, languageCode);
                
            if (isAccommodationFailure)
                return Result.Failure<BookingVoucherData>(accommodationError.Detail);

            var bannerMaybe = await _imageFileService.GetBanner(agent);
            var logoMaybe = await _imageFileService.GetLogo(agent);

            if (!AvailableForVoucherBookingStatuses.Contains(booking.Status))
                return Result.Failure<BookingVoucherData>($"Voucher is not allowed for booking status '{EnumFormatters.FromDescription(booking.Status)}'");

            if (!AvailableForVoucherPaymentStatuses.Contains(booking.PaymentStatus))
                return Result.Failure<BookingVoucherData>($"Voucher is not allowed for payment status '{EnumFormatters.FromDescription(booking.PaymentStatus)}'");

            return new BookingVoucherData
            (
                $"{agent.FirstName} {agent.LastName}",
                booking.Id,
                GetAccommodationInfo(in accommodationDetails),
                (booking.CheckOutDate - booking.CheckInDate).Days,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.DeadlineDate,
                booking.MainPassengerName,
                booking.ReferenceCode,
                bannerMaybe.HasValue ? bannerMaybe.Value.Url : null,
                logoMaybe.HasValue ? logoMaybe.Value.Url : null,
                booking.Rooms.Select(r=> new BookingVoucherData.RoomInfo(r.ContractDescription,
                    r.BoardBasis,
                    r.MealPlan,
                    r.DeadlineDate,
                    r.ContractDescription,
                    r.Passengers,
                    r.Remarks,
                    r.SupplierRoomReferenceCode))
                    .ToList()
            ); 
        }
        
        private static BookingVoucherData.AccommodationInfo GetAccommodationInfo(in Accommodation details)
        {
            var location = new SlimLocationInfo(details.Location.Address, details.Location.Country, details.Location.CountryCode, details.Location.Locality, details.Location.LocalityZone, details.Location.Coordinates);
            return new BookingVoucherData.AccommodationInfo(details.Name, in location, 
                details.Contacts, details.Schedule.CheckInTime, details.Schedule.CheckOutTime);
        }


        public async Task<Result<(DocumentRegistrationInfo RegistrationInfo, BookingInvoiceData Data)>> GetActualInvoice(int bookingId, int agentId, int agencyId)
        {
            var permissions = await _context.AgentAgencyRelations
                .Where(a => a.AgencyId == agencyId && a.AgentId == agentId)
                .Select(a => a.InAgencyPermissions)
                .SingleOrDefaultAsync();
            
            if (permissions == default)
                return Result.Failure<(DocumentRegistrationInfo Metadata, BookingInvoiceData Data)>($"Could not find agent {agentId} in agency {agencyId}");
            
            var getBookingTask = permissions.HasFlag(InAgencyPermissions.AgencyBookingsManagement) 
                ? _bookingRecordsManager.GetByAgency(bookingId, agencyId) 
                : _bookingRecordsManager.GetByAgent(bookingId, agentId);
            
            var (_, isFailure, booking, _) = await getBookingTask;
            if (isFailure)
                return Result.Failure<(DocumentRegistrationInfo Metadata, BookingInvoiceData Data)>("Could not find booking");

            return await GetActualInvoice(booking);
        }


        public async Task<Result> GenerateInvoice(string referenceCode)
        {
            var (_, isBookingFailure, booking, bookingError) = await _bookingRecordsManager.Get(referenceCode);
            if (isBookingFailure)
                return Result.Failure(bookingError);

            var (_, isCounterpartyFailure, counterparty, counterpartyError) = await _counterpartyService.Get(booking.CounterpartyId);
            if (isCounterpartyFailure)
                return Result.Failure(counterpartyError);

            var invoiceData = new BookingInvoiceData(
                GetBuyerInfo(in counterparty),
                GetSellerDetails(booking, _bankDetails),
                booking.ReferenceCode,
                GetRows(booking.AccommodationName, booking.Rooms),
                new MoneyAmount(booking.TotalPrice, booking.Currency),
                booking.DeadlineDate ?? booking.CheckInDate,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.PaymentStatus,
                booking.DeadlineDate
            );

            await _invoiceService.Register(ServiceTypes.HTL, ServiceSource.Internal, booking.ReferenceCode, invoiceData);
            return Result.Success();


            static List<BookingInvoiceData.InvoiceItemInfo> GetRows(string accommodationName, List<BookedRoom> bookingRooms)
            {
                return bookingRooms
                    .Select((room, counter) =>
                        new BookingInvoiceData.InvoiceItemInfo(counter + 1,
                            accommodationName,
                            room.ContractDescription,
                            room.Price,
                            room.Price,
                            room.Type,
                            room.DeadlineDate,
                            room.Passengers.Where(p => p.IsLeader).Select(p => p.FirstName).SingleOrDefault(),
                            room.Passengers.Where(p => p.IsLeader).Select(p => p.LastName).SingleOrDefault()
                        ))
                    .ToList();
            }


            static BookingInvoiceData.SellerInfo GetSellerDetails(Booking booking, BankDetails bankDetails)
            {
                if (!bankDetails.AccountDetails.TryGetValue(booking.Currency, out var accountData))
                    accountData = bankDetails.AccountDetails[Currencies.USD];

                var sellerDetails = new BookingInvoiceData.SellerInfo(bankDetails.CompanyName,
                    bankDetails.BankName, 
                    bankDetails.BankAddress,
                    accountData.AccountNumber,
                    accountData.Iban, 
                    bankDetails.RoutingCode,
                    bankDetails.SwiftCode);
                
                return sellerDetails;
            }

            static BookingInvoiceData.BuyerInfo GetBuyerInfo(in CounterpartyInfo counterparty) => new BookingInvoiceData.BuyerInfo(counterparty.Name, 
                counterparty.Address,
                counterparty.Phone,
                counterparty.BillingEmail);
        }


        public async Task<Result<(DocumentRegistrationInfo RegistrationInfo, PaymentReceipt Data)>> GenerateReceipt(int bookingId, int agentId)
        {
            var agent = await _context.Agents.SingleOrDefaultAsync(a => a.Id == agentId);

            var (_, isBookingFailure, booking, bookingError) = await _bookingRecordsManager.Get(bookingId);
            if (isBookingFailure)
                return Result.Failure<(DocumentRegistrationInfo, PaymentReceipt)>(bookingError);
            
            var (_, isInvoiceFailure, invoiceInfo, invoiceError) = await GetActualInvoice(booking);
            if (isInvoiceFailure)
                return Result.Failure<(DocumentRegistrationInfo, PaymentReceipt)>(invoiceError);
            
            var receiptData = new PaymentReceipt(booking.TotalPrice, 
                booking.Currency,
                booking.PaymentMethod,
                booking.ReferenceCode,
                invoiceInfo.RegistrationInfo,
                booking.AccommodationName,
                booking.CheckInDate,
                booking.CheckOutDate,
                booking.DeadlineDate,
                booking.Rooms.Select(r => new PaymentReceipt.ReceiptItemInfo(r.DeadlineDate, r.Type)).ToList(),
                new PaymentReceipt.BuyerInfo(
                    invoiceInfo.Data.BuyerDetails.Name,
                    invoiceInfo.Data.BuyerDetails.Address,
                    invoiceInfo.Data.BuyerDetails.ContactPhone,
                    invoiceInfo.Data.BuyerDetails.Email),
                $"{agent.FirstName} {agent.LastName}");

            var (_, isRegistrationFailure, regInfo, registrationError) = await _receiptService.Register(invoiceInfo.RegistrationInfo.Number, receiptData);
            if (isRegistrationFailure)
                return Result.Failure<(DocumentRegistrationInfo, PaymentReceipt)>(registrationError);
                
            return Result.Success((regInfo, receiptData));
        }
        
        
        private async Task<Result<(DocumentRegistrationInfo RegistrationInfo, BookingInvoiceData Data)>> GetActualInvoice(Booking booking)
        {
            var lastInvoice = (await _invoiceService.Get<BookingInvoiceData>(ServiceTypes.HTL, ServiceSource.Internal, booking.ReferenceCode))
                .OrderByDescending(i => i.Metadata.Date)
                .LastOrDefault();

            return lastInvoice.Equals(default)
                ? Result.Failure<(DocumentRegistrationInfo Metadata, BookingInvoiceData Data)>("Could not find invoice")
                : Result.Success(lastInvoice);
        }


        private static readonly HashSet<BookingStatuses> AvailableForVoucherBookingStatuses = new()
        {
            BookingStatuses.Confirmed
        };

        private static readonly HashSet<BookingPaymentStatuses> AvailableForVoucherPaymentStatuses = new()
        {
            BookingPaymentStatuses.Authorized,
            BookingPaymentStatuses.Captured
        };

        private readonly EdoContext _context;
        private readonly BankDetails _bankDetails;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly IAccommodationService _accommodationService;
        private readonly ICounterpartyService _counterpartyService;
        private readonly IInvoiceService _invoiceService;
        private readonly IReceiptService _receiptService;
        private readonly IImageFileService _imageFileService;
    }
}