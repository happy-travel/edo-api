using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Services.Accommodations.Bookings.Management;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.General;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EnumFormatters = HappyTravel.Formatters.EnumFormatters;
using MoneyFormatter = HappyTravel.Formatters.MoneyFormatter;
using DateTimeFormatters = HappyTravel.Formatters.DateTimeFormatters;
using HappyTravel.Edo.Api.Services.Notifications;
using System.Text.Json;
using HappyTravel.Edo.Notifications.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings.Mailing
{
    public class BookingNotificationService : IBookingNotificationService
    {
        public BookingNotificationService(IBookingRecordManager bookingRecordManager, 
            MailSenderWithCompanyInfo mailSender,
            ISendingNotificationsService sendingNotificationsService,
            IOptions<BookingMailingOptions> options,
            EdoContext context)
        {
            _bookingRecordManager = bookingRecordManager;
            _mailSender = mailSender;
            _sendingNotificationsService = sendingNotificationsService;
            _options = options.Value;
            _context = context;
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
            return _bookingRecordManager.Get(bookingId)
                .Bind(async booking => 
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

                    // TODO: We are now sending parameters for mail, but they are not used in NotificationCenter.
                    // Sending by email via NotificationCenter will be implemented in the task AA-128.
                    await _sendingNotificationsService.Send(agent: new Models.Agents.SlimAgentContext(agentId: booking.AgentId, agencyId: booking.AgencyId),
                                message: JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(deadlineData, new(JsonSerializerDefaults.Web))),
                                notificationType: NotificationTypes.DeadlineApproaching,
                                email: email,
                                templateId: _options.DeadlineNotificationTemplateId);

                    // TODO: This line will be removed after implementing the task AA-128.
                    return await SendEmail(email, _options.DeadlineNotificationTemplateId, deadlineData);
                });
        }


        public async Task<Result> NotifyCreditCardPaymentConfirmed(string referenceCode)
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
                        LeadingPassenger = booking.GetLeadingPassengerFormattedName(),
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


        public async Task NotifyBookingManualCorrectionNeeded(string referenceCode, string agentName, string agencyName, string deadline)
        {
            await _mailSender.Send(_options.BookingManualCorrectionNeededTemplateId, _options.CcNotificationAddresses, new BookingManualCorrectionNeededData
            {
                ReferenceCode = referenceCode,
                AgentName = agentName,
                AgencyName = agencyName,
                Deadline = deadline
            });
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


        private readonly IBookingRecordManager _bookingRecordManager;
        private readonly MailSenderWithCompanyInfo _mailSender;
        private readonly ISendingNotificationsService _sendingNotificationsService;
        private readonly BookingMailingOptions _options;
        private readonly EdoContext _context;
    }
}