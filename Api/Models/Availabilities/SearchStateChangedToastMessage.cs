using System;

namespace HappyTravel.Edo.Api.Models.Availabilities
{
    public readonly struct SearchStateChangedMessage
    {
        public SearchStateChangedMessage(Guid searchId)
        {
            SearchId = searchId;
        }
        
        public Guid SearchId { get; }
    }
}