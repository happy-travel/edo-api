using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IDataProvider
    {
        Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode);


        Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailability(long availabilityId,
            string accommodationId, string languageCode);


        Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(string accommodationId, string availabilityId, string agreementCode, string languageCode);

        Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(string accommodationId, string languageCode);


        Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(long availabilityId, string agreementId,
            string languageCode);


        Task<Result<BookingDetails, ProblemDetails>> Book(BookingRequest request, string languageCode);

        Task<Result<VoidObject, ProblemDetails>> CancelBooking(string referenceCode);
    }
}