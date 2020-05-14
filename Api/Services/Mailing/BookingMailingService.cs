using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.MailSender;
using HappyTravel.MailSender.Formatters;
using HappyTravel.Money.Models;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Mailing
{
    public class BookingMailingService : IBookingMailingService
    {
        public BookingMailingService(IMailSender mailSender,
            IBookingDocumentsService bookingDocumentsService,
            IOptions<BookingMailingOptions> options)
        {
            _bookingDocumentsService = bookingDocumentsService;
            _mailSender = mailSender;
            _options = options.Value;
        }


        public Task<Result> SendVoucher(int bookingId, string email, RequestMetadata requestMetadata)
        {
            return _bookingDocumentsService.GenerateVoucher(bookingId, requestMetadata)
                .OnSuccess(voucher =>
                {
                    var voucherData = new
                    {
                        Accommodation = voucher.Accommodation,
                        AgentName = voucher.AgentName,
                        BookingId = voucher.BookingId,
                        DeadlineDate = FormatDate(voucher.DeadlineDate),
                        NightCount = voucher.NightCount,
                        ReferenceCode = voucher.ReferenceCode,
                        RoomDetails = voucher.RoomDetails,
                        CheckInDate = FormatDate(voucher.CheckInDate),
                        CheckOutDate = FormatDate(voucher.CheckOutDate),
                        MainPassengerName = voucher.MainPassengerName
                    };
                    
                    return SendEmail(email, _options.VoucherTemplateId, voucherData);
                });
        }


        public Task<Result> SendInvoice(int bookingId, string email, string languageCode)
        {
            return _bookingDocumentsService.GenerateInvoice(bookingId, languageCode)
                .OnSuccess(invoice =>
                {
                    var invoiceData = new
                    {
                        Id = invoice.Id,
                        BuyerDetails = invoice.BuyerDetails,
                        InvoiceDate = FormatDate(invoice.InvoiceDate),
                        InvoiceItems = invoice.InvoiceItems
                            .Select(i=> new
                            {
                                Number = i.Number,
                                Price = FormatPrice(i.Price),
                                Total = FormatPrice(i.Total),
                                AccommodationName = i.AccommodationName,
                                RoomDescription = i.RoomDescription
                            })
                            .ToList(),
                        TotalPrice = FormatPrice(invoice.TotalPrice),
                        CurrencyCode = invoice.TotalPrice.Currency.ToString(),
                        ReferenceCode = invoice.ReferenceCode,
                        SellerDetails = invoice.SellerDetails,
                        PayDueDate = FormatDate(invoice.PayDueDate),
                    };
                    
                    return SendEmail(email, _options.InvoiceTemplateId, invoiceData);
                });
        }


        public Task<Result> NotifyBookingCancelled(string referenceCode, string email, string agentName)
            => _mailSender.Send(_options.BookingCancelledTemplateId, email, new
            {
                agentName,
                referenceCode
            });


        private Task<Result> SendEmail<T>(string email, string templateId, T data)
        {
            return Validate()
                .OnSuccess(Send);


            Result Validate() => GenericValidator<string>
                .Validate(setup => setup.RuleFor(e => e).EmailAddress(), email);


            Task<Result> Send() => _mailSender.Send(templateId, email, data);
        }
        
        
        private static string FormatDate(DateTime? date)
        {
            return date.HasValue
                ? date.Value.ToString("dd MMMM yyyy")
                : string.Empty;
        }


        private static string FormatPrice(MoneyAmount moneyAmount) => EmailContentFormatter.FromAmount(moneyAmount.Amount, moneyAmount.Currency);


        private readonly IBookingDocumentsService _bookingDocumentsService;
        private readonly IMailSender _mailSender;
        private readonly BookingMailingOptions _options;
    }
}