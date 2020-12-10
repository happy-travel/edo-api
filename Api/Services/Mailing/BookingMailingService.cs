using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Payments.Accounts;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.Edo.Data.Documents;
using HappyTravel.EdoContracts.General;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Formatters;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EnumFormatters = HappyTravel.Formatters.EnumFormatters;
using MoneyFormatter = HappyTravel.Formatters.MoneyFormatter;
using DateTimeFormatters = HappyTravel.Formatters.DateTimeFormatters;

namespace HappyTravel.Edo.Api.Services.Mailing
{
    public class BookingMailingService : IBookingMailingService
    {
        public BookingMailingService(MailSenderWithCompanyInfo mailSender,
            IBookingDocumentsService bookingDocumentsService,
            IBookingRecordsManager bookingRecordsManager,
            IOptions<BookingMailingOptions> options,
            IDateTimeProvider dateTimeProvider,
            IAgentSettingsManager agentSettingsManager,
            IAccountPaymentService accountPaymentService,
            EdoContext context)
        {
            _bookingDocumentsService = bookingDocumentsService;
            _bookingRecordsManager = bookingRecordsManager;
            _mailSender = mailSender;
            _options = options.Value;
            _dateTimeProvider = dateTimeProvider;
            _agentSettingsManager = agentSettingsManager;
            _accountPaymentService = accountPaymentService;
            _context = context;
        }


        public Task<Result> SendVoucher(int bookingId, string email, AgentContext agent, string languageCode)
        {
            return _bookingDocumentsService.GenerateVoucher(bookingId, agent, languageCode)
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

                    return SendEmail(email, _options.VoucherTemplateId, voucherData);
                });
        }


        public Task<Result> SendInvoice(int bookingId, string email, int agentId)
        {
            // TODO: hardcoded to be removed with UEDA-20
            var addresses = new List<string> {email};
            addresses.AddRange(_options.CcNotificationAddresses);

            return CheckBookingStatus()
                .Bind(GetActualInvoice)
                .Bind(Send);


            async Task<Result> CheckBookingStatus()
            {
                var isAllowed = await _context.Bookings
                    .AnyAsync(b => b.Id == bookingId && !NotAvailableForSentInvoiceStatuses.Contains(b.Status));

                return isAllowed
                    ? Result.Success()
                    : Result.Failure($"Sent invoice for booking id {bookingId} not allowed");
            }


            Task<Result<(DocumentRegistrationInfo RegistrationInfo, BookingInvoiceData Data)>> GetActualInvoice()
                => _bookingDocumentsService.GetActualInvoice(bookingId, agentId);


            Task<Result> Send((DocumentRegistrationInfo RegistrationInfo, BookingInvoiceData Data) invoice)
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
                            RoomType = EnumFormatters.FromDescription(i.RoomType),
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
            }
        }


        public async Task NotifyBookingCancelled(AccommodationBookingInfo bookingInfo)
        {
            var agentNotificationTemplate = _options.BookingCancelledTemplateId;
            await SendDetailedBookingNotification(bookingInfo, bookingInfo.AgentInformation.AgentEmail, agentNotificationTemplate);
            
            var adminNotificationTemplate = _options.ReservationsBookingCancelledTemplateId;
            await SendDetailedBookingNotification(bookingInfo, _options.CcNotificationAddresses, adminNotificationTemplate);
        }


        // TODO: hardcoded to be removed with UEDA-20
        public async Task NotifyBookingFinalized(AccommodationBookingInfo bookingInfo)
        {
            var agentNotificationTemplate = _options.BookingFinalizedTemplateId;
            await SendDetailedBookingNotification(bookingInfo, bookingInfo.AgentInformation.AgentEmail, agentNotificationTemplate);
            
            var adminNotificationTemplate = _options.ReservationsBookingFinalizedTemplateId;
            await SendDetailedBookingNotification(bookingInfo, _options.CcNotificationAddresses, adminNotificationTemplate);
        }


        public Task<Result> NotifyDeadlineApproaching(int bookingId, string email)
        {
            return _bookingRecordsManager.Get(bookingId)
                .Bind(booking =>
                {
                    var roomDescriptions = booking.Rooms
                        .Select(r => r.ContractDescription);

                    var passengers = booking.Rooms
                        .SelectMany(r => r.Passengers)
                        .Select(p => $"{p.FirstName} {p.LastName}");
                    
                    var deadlineData = new BookingDeadlineData
                    {
                        BookingId = booking.Id,
                        RoomDescriptions = string.Join(", ", roomDescriptions),
                        Passengers = string.Join(", ", passengers),
                        ReferenceCode = booking.ReferenceCode,
                        CheckInDate = DateTimeFormatters.ToDateString(booking.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(booking.CheckOutDate),
                        Deadline = DateTimeFormatters.ToDateString(booking.DeadlineDate)
                    };
                    
                    return SendEmail(email, _options.DeadlineNotificationTemplateId, deadlineData);
                });
        }


        public async Task<Result<string>> SendBookingReports(int agencyId)
        {
            DateTime reportBeginDate = _dateTimeProvider.UtcNow().Date;

            return await GetEmailsAndSettings()
                .Map(GetBookings)
                .Bind(CreateMailData)
                .Bind(SendMails);
            

            async Task<Result<List<EmailAndSetting>>> GetEmailsAndSettings()
            {
                var emailsAndSettings = await
                    (from relation in _context.AgentAgencyRelations
                        join agent in _context.Agents
                            on relation.AgentId equals agent.Id
                        where relation.AgencyId == agencyId
                            && relation.InAgencyPermissions.HasFlag(InAgencyPermissions.ReceiveBookingSummary)
                        select new EmailAndSetting
                        {
                            Email = agent.Email, 
                            ReportDaysSetting = _agentSettingsManager.GetUserSettings(agent).BookingReportDays
                        }).ToListAsync();

                return emailsAndSettings.Any()
                    ? Result.Success(emailsAndSettings)
                    : Result.Failure<List<EmailAndSetting>>($"Couldn't find any agents in agency with id {agencyId} to send summary to");
            }


            async Task<(List<EmailAndSetting>, List<Booking>)> GetBookings(List<EmailAndSetting> emailsAndSettings)
            {
                var maxPeriod = emailsAndSettings.Max(t => t.ReportDaysSetting);
                var reportMaxEndDate = reportBeginDate.AddDays(maxPeriod);

                var bookings = await _context.Bookings.Where(b => b.AgencyId == agencyId
                    && b.PaymentMethod == PaymentMethods.BankTransfer
                    && b.PaymentStatus != BookingPaymentStatuses.Captured
                    && BookingStatusesForSummary.Contains(b.Status)
                    && b.DeadlineDate < reportMaxEndDate).ToListAsync();

                return (emailsAndSettings, bookings);
            }


            async Task<Result<List<(BookingSummaryNotificationData, string)>>> CreateMailData((List<EmailAndSetting> emailsAndSettings, List<Booking> bookings) values)
            {
                var (_, isFailure, balanceInfo, error) = await _accountPaymentService.GetAccountBalance(Currencies.USD, agencyId);
                if (isFailure)
                    return Result.Failure<List<(BookingSummaryNotificationData, string)>>(
                        $"Couldn't retrieve account balance for agency with id {agencyId}. Error: {error}");

                var agencyBalance = balanceInfo.Balance;

                return values.emailsAndSettings.Select(emailAndSetting =>
                {
                    var reportEndDate = reportBeginDate.AddDays(emailAndSetting.ReportDaysSetting);
                    var includedBookings = values.bookings.Where(b => b.DeadlineDate < reportEndDate).ToList();

                    var resultingBalance = agencyBalance - includedBookings.Sum(b => b.TotalPrice);

                    return (new BookingSummaryNotificationData
                        {
                            Bookings = includedBookings.OrderBy(b => b.DeadlineDate).Select(CreateBookingData).ToList(),
                            CurrentBalance = MoneyFormatter.ToCurrencyString(agencyBalance, Currencies.USD),
                            ResultingBalance = MoneyFormatter.ToCurrencyString(resultingBalance, Currencies.USD),
                            ShowAlert = resultingBalance < 0m,
                            ReportDate = DateTimeFormatters.ToDateString(reportEndDate)
                        },
                        emailAndSetting.Email);
                }).Where(t => t.Item1.Bookings.Any()).ToList();


                static BookingSummaryNotificationData.BookingData CreateBookingData(Booking booking) =>
                    new BookingSummaryNotificationData.BookingData
                    {
                        ReferenceCode = booking.ReferenceCode,
                        Accommodation = booking.AccommodationName,
                        Location = $"{booking.Location.Country}, {booking.Location.Locality}",
                        LeadingPassenger = GetLeadingPassengerFormattedName(booking),
                        Amount = MoneyFormatter.ToCurrencyString(booking.TotalPrice, booking.Currency),
                        DeadlineDate = DateTimeFormatters.ToDateString(booking.DeadlineDate),
                        CheckInDate = DateTimeFormatters.ToDateString(booking.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(booking.CheckOutDate),
                        Status = EnumFormatters.FromDescription(booking.Status)
                    };
            }


            async Task<Result<string>> SendMails(List<(BookingSummaryNotificationData Data, string Email)> dataAndEmailTuples)
            {
                var builder = new StringBuilder();
                var hasErrors = false;

                foreach (var (data, email) in dataAndEmailTuples)
                {
                    var (_, isFailure, error) = await SendEmail(email, _options.BookingSummaryTemplateId, data);
                    if (isFailure)
                        hasErrors = true;

                    var message = isFailure
                        ? $"Failed to send a booking summary report for agency with id {agencyId} to '{email}'. Error: {error}"
                        : $"Successfully sent a booking summary report for agency with id {agencyId} to '{email}'";

                    builder.AppendLine(message);
                }

                return hasErrors
                    ? Result.Failure<string>(builder.ToString())
                    : Result.Success(builder.ToString());
            }
        }


        public Task<Result> SendBookingsAdministratorSummary()
        {
            return GetNotificationData()
                .Bind(Send);
            
            
            async Task<Result<BookingAdministratorSummaryNotificationData>> GetNotificationData()
            {
                var startDate = _dateTimeProvider.UtcToday();
                var endDate = startDate.AddDays(DayBeforeAdministratorsNotification);
                
                var bookingRowsQuery = from booking in _context.Bookings
                    join agent in _context.Agents on booking.AgentId equals agent.Id
                    join agentAgencyRelation in _context.AgentAgencyRelations on agent.Id equals agentAgencyRelation.AgentId
                    join agency in _context.Agencies on agentAgencyRelation.AgencyId equals agency.Id
                    where ((booking.CheckInDate <= endDate && booking.CheckInDate >= startDate) ||
                            booking.DeadlineDate.HasValue && booking.DeadlineDate >= startDate && booking.DeadlineDate <= endDate)
                    orderby booking.DeadlineDate ?? booking.CheckInDate    
                    select new BookingAdministratorSummaryNotificationData.BookingRowData()
                    {
                        Agency = agency.Name,
                        Agent = $"{agent.FirstName} {agent.LastName}",
                        ReferenceCode = booking.ReferenceCode,
                        Accommodation = booking.AccommodationName,
                        Location = $"{booking.Location.Country}, {booking.Location.Locality}",
                        LeadingPassenger = GetLeadingPassengerFormattedName(booking),
                        Amount = MoneyFormatter.ToCurrencyString(booking.TotalPrice, booking.Currency),
                        DeadlineDate = DateTimeFormatters.ToDateString(booking.DeadlineDate),
                        CheckInDate = DateTimeFormatters.ToDateString(booking.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(booking.CheckOutDate),
                        Status = EnumFormatters.FromDescription(booking.Status),
                        PaymentStatus = EnumFormatters.FromDescription(booking.PaymentStatus)
                    };
            
                return new BookingAdministratorSummaryNotificationData
                {
                    ReportDate = DateTimeFormatters.ToDateString(_dateTimeProvider.UtcToday()),
                    Bookings = await bookingRowsQuery.ToListAsync()
                };
            }
            
            
            Task<Result> Send(BookingAdministratorSummaryNotificationData notificationData) => _mailSender.Send(_options.BookingAdministratorSummaryTemplateId, _options.CcNotificationAddresses, notificationData);
        }


        public async Task<Result> SendBookingsPaymentsSummaryToAdministrator()
        {
            if (_dateTimeProvider.UtcToday().Day != MonthlyReportScheduleDay)
                return Result.Success();
            
            return await GetNotificationData()
                .Bind(Send);

            
            async Task<Result<BookingAdministratorSummaryNotificationData>> GetNotificationData()
            {
                var startDate = _dateTimeProvider.UtcToday().AddMonths(-1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                
                var bookingRowsQuery = from booking in _context.Bookings
                    join agent in _context.Agents on booking.AgentId equals agent.Id
                    join agentAgencyRelation in _context.AgentAgencyRelations on agent.Id equals agentAgencyRelation.AgentId
                    join agency in _context.Agencies on agentAgencyRelation.AgencyId equals agency.Id
                    where ((booking.CheckInDate <= endDate && booking.CheckInDate >= startDate) ||
                            booking.DeadlineDate.HasValue && booking.DeadlineDate >= startDate && booking.DeadlineDate <= endDate)
                    where booking.PaymentMethod == PaymentMethods.BankTransfer
                    orderby booking.DeadlineDate ?? booking.CheckInDate    
                    select new BookingAdministratorSummaryNotificationData.BookingRowData
                    {
                        Agency = agency.Name,
                        Agent = $"{agent.FirstName} {agent.LastName}",
                        ReferenceCode = booking.ReferenceCode,
                        Accommodation = booking.AccommodationName,
                        Location = $"{booking.Location.Country}, {booking.Location.Locality}",
                        LeadingPassenger = GetLeadingPassengerFormattedName(booking),
                        Amount = MoneyFormatter.ToCurrencyString(booking.TotalPrice, booking.Currency),
                        DeadlineDate = DateTimeFormatters.ToDateString(booking.DeadlineDate),
                        CheckInDate = DateTimeFormatters.ToDateString(booking.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(booking.CheckOutDate),
                        Status = EnumFormatters.FromDescription(booking.Status),
                        PaymentStatus = EnumFormatters.FromDescription(booking.PaymentStatus)
                    };
            
                return new BookingAdministratorSummaryNotificationData
                {
                    ReportDate = DateTimeFormatters.ToDateString(_dateTimeProvider.UtcToday()),
                    Bookings = await bookingRowsQuery.ToListAsync()
                };
            }
            
            
            Task<Result> Send(BookingAdministratorSummaryNotificationData notificationData)
            {
                return _mailSender.Send(_options.BookingAdministratorPaymentsSummaryTemplateId, 
                    _options.CcNotificationAddresses, notificationData);
            }
        }


        public async Task<Result> SendCreditCardPaymentNotifications(string referenceCode)
        {
            return await GetData()
                .Tap(SendNotifyToAdmin)
                .Tap(SendNotifyToAgent);


            async Task<Result<CreditCardPaymentConfirmationNotification>> GetData()
            {
                var query = from booking in _context.Bookings
                    join agent in _context.Agents on booking.AgentId equals agent.Id
                    join agentAgencyRelation in _context.AgentAgencyRelations on agent.Id equals agentAgencyRelation.AgentId
                    join agency in _context.Agencies on agentAgencyRelation.AgencyId equals agency.Id
                    where booking.ReferenceCode == referenceCode
                    select new CreditCardPaymentConfirmationNotification
                    {
                        Agency = agency.Name,
                        Agent = $"{agent.FirstName} {agent.LastName}",
                        ReferenceCode = booking.ReferenceCode,
                        Accommodation = booking.AccommodationName,
                        Location = $"{booking.Location.Country}, {booking.Location.Locality}",
                        LeadingPassenger = GetLeadingPassengerFormattedName(booking),
                        Amount = MoneyFormatter.ToCurrencyString(booking.TotalPrice, booking.Currency),
                        DeadlineDate = DateTimeFormatters.ToDateString(booking.DeadlineDate),
                        CheckInDate = DateTimeFormatters.ToDateString(booking.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(booking.CheckOutDate),
                        Status = EnumFormatters.FromDescription(booking.Status),
                        PaymentStatus = EnumFormatters.FromDescription(booking.PaymentStatus),
                        Email = agent.Email
                    };

                var data = await query.SingleOrDefaultAsync();

                return data ?? Result.Failure<CreditCardPaymentConfirmationNotification>($"Booking with reference code {referenceCode} not found");
            }


            Task SendNotifyToAdmin(CreditCardPaymentConfirmationNotification data)
                => _mailSender.Send(_options.AdminCreditCardPaymentConfirmationTemplateId, _options.CcNotificationAddresses, data);


            Task SendNotifyToAgent(CreditCardPaymentConfirmationNotification data)
                => _mailSender.Send(_options.AgentCreditCardPaymentConfirmationTemplateId, data.Email, data);
        }


        private Task<Result> SendEmail(string email, string templateId, DataWithCompanyInfo data)
        {
            return Validate()
                .Bind(Send);


            Result Validate()
                => GenericValidator<string>
                    .Validate(setup => setup.RuleFor(e => e).EmailAddress(), email);


            Task<Result> Send() => _mailSender.Send(templateId, email, data);
        }


        private Task SendDetailedBookingNotification(AccommodationBookingInfo bookingInfo, string recipient, string mailTemplate)
        {
            var recipients = new List<string> {recipient};
            return SendDetailedBookingNotification(bookingInfo, recipients, mailTemplate);
        }

        
        private Task SendDetailedBookingNotification(AccommodationBookingInfo bookingInfo, List<string> recipients, string mailTemplate)
        {
            var details = bookingInfo.BookingDetails;
            var notificationData = CreateNotificationData(bookingInfo, details);
            return _mailSender.Send(mailTemplate, recipients, notificationData);


            static BookingNotificationData CreateNotificationData(AccommodationBookingInfo bookingInfo, AccommodationBookingDetails details)
            {
                return new BookingNotificationData
                {
                    AgentName = bookingInfo.AgentInformation.AgentName,
                    BookingDetails = new BookingNotificationData.Details
                    {
                        AccommodationName = details.AccommodationName,
                        CheckInDate = DateTimeFormatters.ToDateString(details.CheckInDate),
                        CheckOutDate = DateTimeFormatters.ToDateString(details.CheckOutDate),
                        DeadlineDate = DateTimeFormatters.ToDateString(details.DeadlineDate),
                        Location = details.Location,
                        NumberOfNights = details.NumberOfNights,
                        NumberOfPassengers = details.NumberOfPassengers,
                        ReferenceCode = details.ReferenceCode,
                        RoomDetails = details.RoomDetails.Select(d =>
                        {
                            var maskedPassengers = d.Passengers.Where(p => p.IsLeader)
                                .Select(p =>
                                {
                                    var firstName = p.FirstName.Length == 1 ? "*" : p.FirstName.Substring(0, 1);
                                    return new Pax(p.Title, p.LastName, firstName);
                                })
                                .ToList();

                            return new BookingNotificationData.BookedRoomDetails
                            {
                                ContractDescription = d.ContractDescription,
                                MealPlan = d.MealPlan,
                                Passengers = maskedPassengers,
                                Price = MoneyFormatter.ToCurrencyString(d.Price.Amount, d.Price.Currency),
                                Type =EnumFormatters.FromDescription(d.Type),
                                Remarks = d.Remarks
                            };
                        }).ToList(),
                        Status = EnumFormatters.FromDescription(details.Status),
                        SupplierReferenceCode = details.AgentReference,
                        ContactInfo = details.ContactInfo,
                    },
                    CounterpartyName = bookingInfo.AgentInformation.CounterpartyName,
                    AgencyName = bookingInfo.AgentInformation.AgencyName,
                    PaymentStatus = EnumFormatters.FromDescription(bookingInfo.PaymentStatus),
                    Price = MoneyFormatter.ToCurrencyString(bookingInfo.TotalPrice.Amount, bookingInfo.TotalPrice.Currency),
                    Supplier = bookingInfo.Supplier is null
                        ? string.Empty
                        : EnumFormatters.FromDescription(bookingInfo.Supplier.Value),
                };
            }
        }

        
        private static string GetLeadingPassengerFormattedName(Booking booking)
        {
            var leadingPassengersList = booking.Rooms
                .SelectMany(r =>
                {
                    if (r.Passengers == null)
                        return new List<Passenger>(0);
                    
                    return r.Passengers.Where(p => p.IsLeader);
                })
                .ToList();
            
            if (leadingPassengersList.Any())
            {
                var leadingPassenger = leadingPassengersList.First();
                return  Formatters.PersonNameFormatters.ToMaskedName(leadingPassenger.FirstName, leadingPassenger.LastName,
                    EnumFormatters.FromDescription(leadingPassenger.Title));
            }

            return Formatters.PersonNameFormatters.ToMaskedName("*", string.Empty);
        }

        
        private static string FormatPrice(MoneyAmount moneyAmount) => MoneyFormatter.ToCurrencyString(moneyAmount.Amount, moneyAmount.Currency);

        private static readonly HashSet<BookingStatuses> BookingStatusesForSummary = new HashSet<BookingStatuses>
        {
            BookingStatuses.Confirmed,
            BookingStatuses.InternalProcessing,
            BookingStatuses.Pending,
            BookingStatuses.WaitingForResponse
        };

        private static readonly HashSet<BookingStatuses> NotAvailableForSentInvoiceStatuses = new HashSet<BookingStatuses>
        {
            BookingStatuses.Cancelled,
            BookingStatuses.Rejected
        };

        private const int DayBeforeAdministratorsNotification = 5;
        private const int MonthlyReportScheduleDay = 1;

        private readonly IBookingDocumentsService _bookingDocumentsService;
        private readonly IBookingRecordsManager _bookingRecordsManager;
        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly BookingMailingOptions _options;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAgentSettingsManager _agentSettingsManager;
        private readonly IAccountPaymentService _accountPaymentService;
        private readonly EdoContext _context;


        private class EmailAndSetting
        {
            public string Email { get; set; }
            public int ReportDaysSetting { get; set; }
        }
    }
}