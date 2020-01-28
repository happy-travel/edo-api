using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Markups.Availability;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Markups.Availability;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AvailabilityService : IAvailabilityService
    {
         public AvailabilityService(ILocationService locationService,
            ICustomerContext customerContext,
            IPermissionChecker permissionChecker,
            IDataProviderClient dataProviderClient,
            IAvailabilityMarkupService markupService,
            IAvailabilityResultsCache availabilityResultsCache,
            IMultiProviderAvailabilityManager multiProviderAvailabilityManager,
            IOptions<DataProviderOptions> options)
        {
            _locationService = locationService;
            _customerContext = customerContext;
            _permissionChecker = permissionChecker;
            _dataProviderClient = dataProviderClient;
            _markupService = markupService;
            _availabilityResultsCache = availabilityResultsCache;
            _multiProviderAvailabilityManager = multiProviderAvailabilityManager;
            _options = options.Value;
        }
        
        
        public async ValueTask<Result<CombinedAvailabilityDetails, ProblemDetails>> GetAvailable(Models.Availabilities.AvailabilityRequest request, string languageCode)
        {
            var (_, isFailure, location, error) = await _locationService.Get(request.Location, languageCode);
            if (isFailure)
                return Result.Fail<CombinedAvailabilityDetails, ProblemDetails>(error);

            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<CombinedAvailabilityDetails>(customerError);

            var (_, permissionDenied, permissionError) =
                await _permissionChecker.CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationAvailabilitySearch);
            if (permissionDenied)
                return ProblemDetailsBuilder.Fail<CombinedAvailabilityDetails>(permissionError);

            return await ExecuteRequest()
                .OnSuccess(ApplyMarkup)
                .OnSuccess(ReturnResponseWithMarkup);


            async Task<Result<CombinedAvailabilityDetails, ProblemDetails>> ExecuteRequest()
            {
                var roomDetails = request.RoomDetails
                    .Select(r => new RoomRequestDetails(r.AdultsNumber, r.ChildrenNumber, r.ChildrenAges, r.Type,
                        r.IsExtraBedNeeded))
                    .ToList();

                var contract = new AvailabilityRequest(request.Nationality, request.Residency, request.CheckInDate,
                    request.CheckOutDate,
                    request.Filters, roomDetails, request.AccommodationIds, location,
                    request.PropertyType, request.Ratings);

                var (isSuccess, _, details, providerError) = await _multiProviderAvailabilityManager.GetAvailability(contract, languageCode);
                return isSuccess
                    ? Result.Ok<CombinedAvailabilityDetails, ProblemDetails>(details)
                    : ProblemDetailsBuilder.Fail<CombinedAvailabilityDetails>(providerError);
            }


            Task<AvailabilityDetailsWithMarkup> ApplyMarkup(CombinedAvailabilityDetails response) => _markupService.Apply(customerInfo, response);

            CombinedAvailabilityDetails ReturnResponseWithMarkup(AvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(string accommodationId, long availabilityId, 
            string languageCode)
        {
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetails>(customerError);

            return await CheckPermissions()
                .OnSuccess(ExecuteRequest)
                .OnSuccess(ApplyMarkup)
                .OnSuccess(ReturnResponseWithMarkup);


            async Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> CheckPermissions()
            {
                var (_, permissionDenied, permissionError) =
                    await _permissionChecker.CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationAvailabilitySearch);
                if (permissionDenied)
                    return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetails>(permissionError);

                return Result.Ok<SingleAccommodationAvailabilityDetails, ProblemDetails>(default);
            }


            Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> ExecuteRequest()
            {
                return _dataProviderClient.Post<SingleAccommodationAvailabilityDetails>(
                    new Uri(_options.Netstorming + "accommodations/" + accommodationId + "/availabilities/" + availabilityId, UriKind.Absolute), languageCode);
            }


            Task<SingleAccommodationAvailabilityDetailsWithMarkup> ApplyMarkup(SingleAccommodationAvailabilityDetails response)
                => _markupService.Apply(customerInfo, response);


            SingleAccommodationAvailabilityDetails ReturnResponseWithMarkup(SingleAccommodationAvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(long availabilityId, Guid agreementId,
            string languageCode)
        {
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetailsWithDeadline>(customerError);

            return await ExecuteRequest()
                .OnSuccess(ApplyMarkup)
                .OnSuccess(SaveToCache)
                .OnSuccess(ReturnResponseWithMarkup);


            Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> ExecuteRequest()
                => _dataProviderClient.Post<SingleAccommodationAvailabilityDetailsWithDeadline>(
                    new Uri($"{_options.Netstorming}accommodations/availabilities/{availabilityId}/agreements/{agreementId}", UriKind.Absolute), languageCode);


            async Task<(SingleAccommodationAvailabilityDetailsWithMarkup, DeadlineDetails)>
                ApplyMarkup(SingleAccommodationAvailabilityDetailsWithDeadline response)
                => (await _markupService.Apply(customerInfo,
                    new SingleAccommodationAvailabilityDetails(
                        response.AvailabilityId,
                        response.CheckInDate,
                        response.CheckOutDate,
                        response.NumberOfNights,
                        response.AccommodationDetails,
                        new List<Agreement> 
                            {response.Agreement})), 
                    response.DeadlineDetails);
                    


            Task SaveToCache((SingleAccommodationAvailabilityDetailsWithMarkup, DeadlineDetails) responseWithDeadline)
            {
                var (availabilityWithMarkup, _) = responseWithDeadline;
                return _availabilityResultsCache.Set(availabilityWithMarkup);
            }


            SingleAccommodationAvailabilityDetailsWithDeadline ReturnResponseWithMarkup(
                (SingleAccommodationAvailabilityDetailsWithMarkup, DeadlineDetails) responseWithDeadline)
            {
                var (availabilityWithMarkup, deadlineDetails) = responseWithDeadline;
                var result = availabilityWithMarkup.ResultResponse;
                return new SingleAccommodationAvailabilityDetailsWithDeadline(
                    result.AvailabilityId,
                    result.CheckInDate,
                    result.CheckOutDate,
                    result.NumberOfNights,
                    result.AccommodationDetails,
                    result.Agreements.SingleOrDefault(),
                    deadlineDetails);
            }
        }
        
        private readonly ILocationService _locationService;
        private readonly ICustomerContext _customerContext;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IDataProviderClient _dataProviderClient;
        private readonly IAvailabilityMarkupService _markupService;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly IMultiProviderAvailabilityManager _multiProviderAvailabilityManager;
        private readonly DataProviderOptions _options;
    }
}