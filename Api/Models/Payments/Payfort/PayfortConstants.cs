using System.Collections.Generic;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Models.Payments.Payfort
{
    public static class PayfortConstants
    {
        public const string PaymentSuccessResponseCode = "14000";
        public const string AuthorizationSuccessResponseCode = "02000";
        public const string PaymentSecure3dResponseCode = "20064";
        public const string CaptureSuccessResponseCode = "04000";
        public const string VoidSuccessResponseCode = "08000";

        public static readonly Dictionary<Currencies, int> ExponentMultipliers = new Dictionary<Currencies, int>
        {
            {Currencies.EUR, 100},
            {Currencies.USD, 100},
            {Currencies.AED, 100},
            {Currencies.SAR, 100}
        };
    }
}
