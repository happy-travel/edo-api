namespace HappyTravel.Edo.Api.Models.Mailing
{
    public class AdministratorCreditCardPaymentConfirmationNotification : DataWithCompanyInfo
    {
        public string ReferenceCode { get; set; }
        public string Accommodation { get; set; }
        public string Location { get; set; }
        public string LeadingPassenger { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }
        public string Amount { get; set; }
        public string DeadlineDate { get; set; }
        public string Status { get; set; }
        public string Agency { get; set; }
        public string Agent { get; set; }
        public string PaymentStatus { get; set; }
    }
}