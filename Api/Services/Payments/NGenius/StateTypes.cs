namespace HappyTravel.Edo.Api.Services.Payments.NGenius
{
    public static class StateTypes
    {
        public const string Authorized = "AUTHORISED";
        public const string Captured = "CAPTURED";
        public const string Await3Ds = "AWAIT_3DS";
        public const string Failed = "FAILED";
    }
}