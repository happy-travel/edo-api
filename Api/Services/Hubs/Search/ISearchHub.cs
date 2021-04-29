using System;
using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Availabilities;

namespace HappyTravel.Edo.Api.Services.Hubs.Search
{
    public interface ISearchHub
    {
        public Task SearchStateChanged(SearchStateChangedMessage message);
    }
}