namespace HappyTravel.Edo.Api.Infrastructure
{
    /// <summary>
    ///     Empty object, used for Result T,E  to provide typed error with no result.
    /// </summary>
    public sealed class Unit
    {
        private Unit()
        { }


        public static readonly Unit Instance = new Unit();
    }
}