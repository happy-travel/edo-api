using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Availabilities;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IAvailabilityResultsCache
    {
        Task Set(DataProviderAvailabilityResponse dataProviderAvailabilityResponse);
        Task<DataProviderAvailabilityResponse> Get(int id);
    }
}