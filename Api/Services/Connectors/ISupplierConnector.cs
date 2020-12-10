using System;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface ISupplierConnector
    {
        Task<Result<Availability, ProblemDetails>> GetAvailability(AvailabilityRequest availabilityRequest, string languageCode);

        Task<Result<AccommodationAvailability, ProblemDetails>> GetAvailability(string availabilityId,
            string accommodationId, string languageCode);
        
        Task<Result<RoomContractSetAvailability?, ProblemDetails>> GetExactAvailability(string availabilityId, Guid roomContractSetId,
            string languageCode);

        Task<Result<Deadline, ProblemDetails>> GetDeadline(string availabilityId, Guid roomContractSetId, string languageCode);

        Task<Result<Accommodation, ProblemDetails>> GetAccommodation(string accommodationId, string languageCode);

        Task<Result<Booking, ProblemDetails>>  Book(BookingRequest request, string languageCode);

        Task<Result<Unit, ProblemDetails>> CancelBooking(string referenceCode);

        Task<Result<Booking, ProblemDetails>> GetBookingDetails(string referenceCode, string languageCode);

        Task<Result<Booking, ProblemDetails>> ProcessAsyncResponse(Stream stream);
    }
}