using HappyTravel.Edo.Data.Markup;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public readonly struct MarkupApplicationResult<TDetails>
    {
        public MarkupApplicationResult(TDetails before, MarkupPolicy policy, TDetails after)
        {
            Before = before;
            Policy = policy;
            After = after;
        }
        
        public TDetails Before { get; }
        public MarkupPolicy Policy { get; }
        public TDetails After { get; }
    }
}