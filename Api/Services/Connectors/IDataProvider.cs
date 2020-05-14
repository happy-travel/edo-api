using System;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IDataProvider
    {
        Task<Result<AvailabilityDetails, ProblemDetails>> GetAvailability(AvailabilityRequest availabilityRequest, RequestMetadata requestMetadata);

        Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailability(string availabilityId,
            string accommodationId, RequestMetadata requestMetadata);
        
        Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> GetExactAvailability(string availabilityId, Guid roomContractSetId, 
            RequestMetadata requestMetadata);

        Task<Result<DeadlineDetails, ProblemDetails>> GetDeadline(string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata);

        Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(string accommodationId, RequestMetadata requestMetadata);

        Task<Result<BookingDetails, ProblemDetails>>  Book(BookingRequest request, RequestMetadata requestMetadata);

        Task<Result<VoidObject, ProblemDetails>> CancelBooking(string referenceCode, RequestMetadata requestMetadata);

        Task<Result<BookingDetails, ProblemDetails>> GetBookingDetails(string referenceCode, RequestMetadata requestMetadata);

        Task<Result<BookingDetails, ProblemDetails>> ProcessAsyncResponse(Stream stream, RequestMetadata requestMetadata);
    }
}