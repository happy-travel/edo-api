using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IMultiProviderAvailabilityManager
    {
        Task<Result<CombinedAvailabilityDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode);
    }
}