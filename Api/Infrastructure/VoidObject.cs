namespace HappyTravel.Edo.Api.Infrastructure
{
    /// <summary>
    /// Empty object, used for Result T,E  to provide typed error with no result.
    /// </summary>
    public sealed class VoidObject
    {
        private VoidObject() {}
        public static readonly VoidObject Instance = new VoidObject();
    }
}