using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Markups.Availability;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Markups.Availability;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AvailabilityService : IAvailabilityService
    {
        public AvailabilityService(IPermissionChecker permissionChecker,
            ILocationService locationService,
            ICustomerContext customerContext,
            IAvailabilityMarkupService markupService,
            IDataProviderFactory dataProviderFactory,
            IAvailabilityResultsCache availabilityResultsCache,
            IMultiProviderAvailabilityManager availabilityManager)
        {
            _permissionChecker = permissionChecker;
            _locationService = locationService;
            _customerContext = customerContext;
            _markupService = markupService;
            _dataProviderFactory = dataProviderFactory;
            _availabilityResultsCache = availabilityResultsCache;
            _availabilityManager = availabilityManager;
        }


        public async ValueTask<Result<AvailabilityDetails, ProblemDetails>> GetAvailable(AvailabilityRequest request, string languageCode)
        {
            var (_, isFailure, location, error) = await _locationService.Get(request.Location, languageCode);
            if (isFailure)
                return Result.Fail<AvailabilityDetails, ProblemDetails>(error);

            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<AvailabilityDetails>(customerError);

            var (_, permissionDenied, permissionError) =
                await _permissionChecker.CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationAvailabilitySearch);
            if (permissionDenied)
                return ProblemDetailsBuilder.Fail<AvailabilityDetails>(permissionError);

            return await ExecuteRequest()
                .OnSuccess(ApplyMarkup)
                .OnSuccess(ReturnResponseWithMarkup);


            async Task<Result<AvailabilityDetails, ProblemDetails>> ExecuteRequest()
            {
                var roomDetails = request.RoomDetails
                    .Select(r => new RoomRequestDetails(r.AdultsNumber, r.ChildrenNumber, r.ChildrenAges, r.Type,
                        r.IsExtraBedNeeded))
                    .ToList();

                var contract = new EdoContracts.Accommodations.AvailabilityRequest(request.Nationality, request.Residency, request.CheckInDate,
                    request.CheckOutDate,
                    request.Filters, roomDetails, request.AccommodationIds, location,
                    request.PropertyType, request.Ratings);

                var (isSuccess, _, result, availabilityError) = await _availabilityManager.GetAvailability(contract, languageCode);
                return isSuccess
                    ? Result.Ok<AvailabilityDetails, ProblemDetails>(result)
                    : ProblemDetailsBuilder.Fail<AvailabilityDetails>(availabilityError);
            }


            Task<AvailabilityDetailsWithMarkup> ApplyMarkup(AvailabilityDetails response) => _markupService.Apply(customerInfo, response);

            AvailabilityDetails ReturnResponseWithMarkup(AvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
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
                // TODO: replace with conditional data provider
                var dataProvider = _dataProviderFactory.Get(DataProviders.Netstorming);
                return dataProvider.GetAvailability(availabilityId, accommodationId, languageCode);
            }


            Task<SingleAccommodationAvailabilityDetailsWithMarkup> ApplyMarkup(SingleAccommodationAvailabilityDetails response)
                => _markupService.Apply(customerInfo, response);


            SingleAccommodationAvailabilityDetails ReturnResponseWithMarkup(SingleAccommodationAvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<SingleAccommodationAvailabilityDetailsWithDeadline, ProblemDetails>> GetExactAvailability(long availabilityId,
            Guid agreementId,
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
            {
                // TODO: replace with conditional data provider
                var dataProvider = _dataProviderFactory.Get(DataProviders.Netstorming);
                return dataProvider.GetExactAvailability(availabilityId, agreementId, languageCode);
            }


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


        private readonly IMultiProviderAvailabilityManager _availabilityManager;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly ICustomerContext _customerContext;
        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly ILocationService _locationService;
        private readonly IAvailabilityMarkupService _markupService;
        private readonly IPermissionChecker _permissionChecker;
    }
}