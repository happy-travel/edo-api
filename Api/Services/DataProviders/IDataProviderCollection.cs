using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Locations;

namespace HappyTravel.Edo.Api.Services.DataProviders
{
    public interface IDataProviderCollection
    {
        IDataProvider Get(string id);
        IEnumerable<IDataProvider> Get();
    }

    public interface IDataProvider
    {
        IDataProviderAccommodationService Accommodations { get; }
    }

    public interface IDataProviderAccommodationService
    {
        Task<Result<DataProviderAvailabilityResponse>> GetAvailable(in AvailabilityRequest request, in Location location);
    }
}