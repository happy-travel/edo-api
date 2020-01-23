using HappyTravel.Edo.Common.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Models.Payments
{
    /// <summary>
    ///     Payment response
    /// </summary>
    public readonly struct PaymentResponse
    {
        [JsonConstructor]
        public PaymentResponse(string secure3d, CreditCardPaymentStatuses status, string message)
        {
            Secure3d = secure3d;
            Status = status;
            Message = message;
        }


        /// <summary>
        ///     3d secure url
        /// </summary>
        public string Secure3d { get; }

        /// <summary>
        ///     Payment status
        /// </summary>
        public CreditCardPaymentStatuses Status { get; }

        /// <summary>
        ///     Payment message from payfort
        /// </summary>
        public string Message { get; }
    }
}