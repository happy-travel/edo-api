using System.Collections.Generic;

namespace HappyTravel.Edo.Api.Infrastructure.Options
{
    public class BookingMailingOptions
    {
        public string VoucherTemplateId { get; set; }
        public string InvoiceTemplateId { get; set; }
        public string BookingCancelledTemplateId { get; set; }
        public string BookingFinalizedTemplateId { get; set; }
        public string DeadlineNotificationTemplateId { get; set; }
        public string ReservationsBookingFinalizedTemplateId { get; set; }
        public string BookingSummaryTemplateId { get; set; }
        public string BookingAdministratorSummaryTemplateId { get; set; }
        public List<string> CcNotificationAddresses { get; set; }
        public string ReservationsBookingCancelledTemplateId { get; set; }
        public string BookingAdministratorPaymentsSummaryTemplateId { get; set; }
        public string CreditCardPaymentConfirmationTemplateId { get; set; }
    }
}