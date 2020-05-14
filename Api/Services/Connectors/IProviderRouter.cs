using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Connectors
{
    public interface IProviderRouter
    {
        Task<Result<CombinedAvailabilityDetails>> GetAvailability(List<DataProviders> dataProviders, AvailabilityRequest availabilityRequest,
            RequestMetadata requestMetadata);


        Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(DataProviders dataProvider, string accommodationId,
            string availabilityId, RequestMetadata requestMetadata);


        Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline?, ProblemDetails>> GetExactAvailability(DataProviders dataProvider, string availabilityId,
            Guid roomContractSetId, RequestMetadata requestMetadata);


        Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(DataProviders dataProvider, string id, RequestMetadata requestMetadata);

        Task<Result<BookingDetails, ProblemDetails>> Book(DataProviders dataProvider, BookingRequest request, RequestMetadata requestMetadata);

        Task<Result<VoidObject, ProblemDetails>> CancelBooking(DataProviders dataProvider, string referenceCode, RequestMetadata requestMetadata);

        Task<Result<DeadlineDetails,ProblemDetails>> GetDeadline(DataProviders dataProvider, string availabilityId, Guid roomContractSetId, RequestMetadata requestMetadata);


        Task<Result<BookingDetails, ProblemDetails>> GetBookingDetails(DataProviders dataProvider, string referenceCode,
            RequestMetadata requestMetadata);
    }
}