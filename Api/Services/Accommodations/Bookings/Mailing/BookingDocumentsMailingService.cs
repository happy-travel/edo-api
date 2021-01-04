using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Models.Payments;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Documents;
using HappyTravel.Edo.Data.Documents;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.Formatters;
using HappyTravel.Money.Models;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing
{
    public class BookingDocumentsMailingService : IBookingDocumentsMailingService
    {
        public BookingDocumentsMailingService(IBookingDocumentsService documentsService,
            MailSenderWithCompanyInfo mailSender,
            IOptions<BookingMailingOptions> options)
        {
            _documentsService = documentsService;
            _mailSender = mailSender;
            _options = options.Value;
        }
        
        
        public Task<Result> SendVoucher(int bookingId, string email, AgentContext agent, string languageCode)
        {
            return _documentsService.GenerateVoucher(bookingId, agent, languageCode)
                .Bind(voucher =>
                {
                    var voucherData = new VoucherData
                    {
                        Accommodation = voucher.Accommodation,
                        AgentName = voucher.AgentName,
                        BookingId = voucher.BookingId,
                        DeadlineDate = DateTimeFormatters.ToDateString(voucher.DeadlineDate),
                        NightCount = voucher.NightCount,
                        ReferenceCode = voucher.ReferenceCode,
                        RoomDetails = voucher.RoomDetails,
                        CheckInDate = DateTimeFormatters.ToDateString(voucher.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(voucher.CheckOutDate),
                        MainPassengerName = voucher.MainPassengerName,
                        BannerUrl = voucher.BannerUrl,
                        LogoUrl = voucher.LogoUrl
                    };
                    
                    return _mailSender.Send(_options.VoucherTemplateId, email, voucherData);
                });
        }


        public Task<Result> SendInvoice(int bookingId, string email, int agentId, int agencyId)
        {
            // TODO: hardcoded to be removed with UEDA-20
            var addresses = new List<string> {email};
            addresses.AddRange(_options.CcNotificationAddresses);
            
            return _documentsService.GetActualInvoice(bookingId, agentId, agencyId)
                .Bind(invoice =>
                {
                    var (registrationInfo, data) = invoice;
                    var invoiceData = new InvoiceData
                    {
                        Number = registrationInfo.Number,
                        BuyerDetails = data.BuyerDetails,
                        InvoiceDate = DateTimeFormatters.ToDateString(registrationInfo.Date),
                        InvoiceItems = data.InvoiceItems
                            .Select(i => new InvoiceData.InvoiceItem
                            {
                                Number = i.Number,
                                Price = FormatPrice(i.Price),
                                Total = FormatPrice(i.Total),
                                AccommodationName = i.AccommodationName,
                                RoomDescription = i.RoomDescription,
                                RoomType = EnumFormatters.FromDescription<RoomTypes>(i.RoomType),
                                DeadlineDate = DateTimeFormatters.ToDateString(i.DeadlineDate),
                                MainPassengerName = PersonNameFormatters.ToMaskedName(i.MainPassengerFirstName, i.MainPassengerLastName)
                            })
                            .ToList(),
                        TotalPrice = FormatPrice(data.TotalPrice),
                        CurrencyCode = EnumFormatters.FromDescription(data.TotalPrice.Currency),
                        ReferenceCode = data.ReferenceCode,
                        SellerDetails = data.SellerDetails,
                        PayDueDate = DateTimeFormatters.ToDateString(data.PayDueDate),
                        CheckInDate = DateTimeFormatters.ToDateString(data.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(data.CheckOutDate),
                        PaymentStatus = EnumFormatters.FromDescription(data.PaymentStatus),
                        DeadlineDate = DateTimeFormatters.ToDateString(data.DeadlineDate)
                    };

                    return _mailSender.Send(_options.InvoiceTemplateId, addresses, invoiceData);
                });
        }
        
        
        public Task<Result> SendReceiptToCustomer((DocumentRegistrationInfo RegistrationInfo, PaymentReceipt Data) receipt, string email)
        {
            var (registrationInfo, paymentReceipt) = receipt;

            var payload = new PaymentReceiptData
            {
                Date = DateTimeFormatters.ToDateString(registrationInfo.Date),
                Number = registrationInfo.Number,
                CustomerName = paymentReceipt.CustomerName,
                Amount = MoneyFormatter.ToCurrencyString(paymentReceipt.Amount, paymentReceipt.Currency),
                Method = EnumFormatters.FromDescription(paymentReceipt.Method),
                InvoiceNumber = paymentReceipt.InvoiceInfo.Number,
                InvoiceDate = DateTimeFormatters.ToDateString(paymentReceipt.InvoiceInfo.Date),
                ReferenceCode = paymentReceipt.ReferenceCode,
                AccommodationName = paymentReceipt.AccommodationName,
                RoomDetails = paymentReceipt.ReceiptItems.Select(r => new PaymentReceiptData.RoomDetail
                {
                    DeadlineDate = r.DeadlineDate,
                    RoomType = r.RoomType
                }).ToList(),
                CheckInDate = DateTimeFormatters.ToDateString(paymentReceipt.CheckInDate),
                CheckOutDate = DateTimeFormatters.ToDateString(paymentReceipt.CheckOutDate),
                BuyerInformation = new PaymentReceiptData.Buyer
                {
                    Address = paymentReceipt.BuyerDetails.Address,
                    ContactPhone = paymentReceipt.BuyerDetails.ContactPhone,
                    Email = paymentReceipt.BuyerDetails.Email,
                    Name = paymentReceipt.BuyerDetails.Name
                },
                DeadlineDate = DateTimeFormatters.ToDateString(paymentReceipt.DeadlineDate)
            };

            return _mailSender.Send(_options.BookingReceiptTemplateId, email, payload);
        }
        
        
        private static string FormatPrice(MoneyAmount moneyAmount) 
            => MoneyFormatter.ToCurrencyString(moneyAmount.Amount, moneyAmount.Currency);

        
        private readonly IBookingDocumentsService _documentsService;
        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly BookingMailingOptions _options;
    }
}