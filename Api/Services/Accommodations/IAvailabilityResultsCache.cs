using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Markups.Availability;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public interface IAvailabilityResultsCache
    {
        Task Set(DataProviders dataProvider, SingleAccommodationAvailabilityDetailsWithMarkup availabilityResponse);

        Task<Result<SingleAccommodationAvailabilityDetailsWithMarkup>> Get(DataProviders dataProvider, long id);
    }
}