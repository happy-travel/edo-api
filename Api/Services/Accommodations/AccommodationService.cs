using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Deadline;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Availability;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.Edo.Data.Infrastructure.DatabaseExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AvailabilityRequest = HappyTravel.EdoContracts.Accommodations.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(IMemoryFlow flow,
            IOptions<DataProviderOptions> options,
            IDataProviderClient dataProviderClient,
            ILocationService locationService,
            IAccommodationBookingManager accommodationBookingManager,
            IAvailabilityResultsCache availabilityResultsCache,
            ICustomerContext customerContext,
            IAvailabilityMarkupService markupService,
            ICancellationPoliciesService cancellationPoliciesService,
            ISupplierOrderService supplierOrderService,
            IMarkupLogger markupLogger,
            IPermissionChecker permissionChecker,
            EdoContext context,
            IPaymentService paymentService,
            ILogger<AccommodationService> logger)
        {
            _flow = flow;
            _dataProviderClient = dataProviderClient;
            _locationService = locationService;
            _accommodationBookingManager = accommodationBookingManager;
            _availabilityResultsCache = availabilityResultsCache;
            _customerContext = customerContext;
            _markupService = markupService;
            _options = options.Value;
            _cancellationPoliciesService = cancellationPoliciesService;
            _supplierOrderService = supplierOrderService;
            _markupLogger = markupLogger;
            _permissionChecker = permissionChecker;
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
        }


        public ValueTask<Result<AccommodationDetails, ProblemDetails>> Get(string accommodationId, string languageCode)
            => _flow.GetOrSetAsync(_flow.BuildKey(nameof(AccommodationService), "Accommodations", languageCode, accommodationId),
                async () => await _dataProviderClient.Get<AccommodationDetails>(
                    new Uri($"{_options.Netstorming}accommodations/{accommodationId}", UriKind.Absolute), languageCode),
                TimeSpan.FromDays(1));


        public async ValueTask<Result<AvailabilityDetails, ProblemDetails>> GetAvailable(Models.Availabilities.AvailabilityRequest request, string languageCode)
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


            Task<Result<AvailabilityDetails, ProblemDetails>> ExecuteRequest()
            {
                var roomDetails = request.RoomDetails
                    .Select(r => new RoomRequestDetails(r.AdultsNumber, r.ChildrenNumber, r.ChildrenAges, r.Type,
                        r.IsExtraBedNeeded))
                    .ToList();

                var contract = new AvailabilityRequest(request.Nationality, request.Residency, request.CheckInDate, request.CheckOutDate,
                    request.Filters, roomDetails, request.AccommodationIds, location,
                    request.PropertyType, request.Ratings);

                return _dataProviderClient.Post<AvailabilityRequest, AvailabilityDetails>(
                    new Uri(_options.Netstorming + "availabilities/accommodations", UriKind.Absolute), contract, languageCode);
            }


            Task<AvailabilityDetailsWithMarkup> ApplyMarkup(AvailabilityDetails response) => _markupService.Apply(customerInfo, response);

            AvailabilityDetails ReturnResponseWithMarkup(AvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<SingleAccommodationAvailabilityDetails, ProblemDetails>> GetAvailable(long availabilityId, string accommodationId,
            string languageCode)
        {
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<SingleAccommodationAvailabilityDetails>(customerError);

            return await CheckPermissions()
                .OnSuccess(ExecuteRequest)
                .OnSuccess(ApplyMarkup)
                .OnSuccess(SaveToCache)
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
                var contract = new SingleAccommodationAvailabilityRequest(availabilityId);

                return _dataProviderClient.Post<SingleAccommodationAvailabilityRequest, SingleAccommodationAvailabilityDetails>(
                    new Uri(_options.Netstorming + "availabilities/accommodations/" + accommodationId, UriKind.Absolute), contract, languageCode);
            }


            Task SaveToCache(SingleAccommodationAvailabilityDetailsWithMarkup availabilityDetailsWithMarkup)
                => _availabilityResultsCache.Set(availabilityDetailsWithMarkup);


            Task<SingleAccommodationAvailabilityDetailsWithMarkup> ApplyMarkup(SingleAccommodationAvailabilityDetails response)
                => _markupService.Apply(customerInfo, response);


            SingleAccommodationAvailabilityDetails ReturnResponseWithMarkup(SingleAccommodationAvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request, string languageCode)
        {
            // TODO: Refactor and simplify method
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(customerError);

            var (_, permissionDenied, permissionError) = await _permissionChecker
                .CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationBooking);
            if (permissionDenied)
                return ProblemDetailsBuilder.Fail<BookingDetails>(permissionError);

            var responseWithMarkup = await _availabilityResultsCache.Get(request.AvailabilityId);
            var (_, isFailure, bookingAvailability, error) =
                await GetBookingAvailability(responseWithMarkup, request.AvailabilityId, request.AgreementId, languageCode);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(error.Detail);

            return await Book()
                .OnSuccess(SaveSupplierOrder)
                .OnSuccess(LogAppliedMarkups)
                .OnSuccess(AuthorizeMoneyFromAccount);

            Task<Result<BookingDetails, ProblemDetails>> Book() => _accommodationBookingManager.Book(request, bookingAvailability, languageCode);


            async Task SaveSupplierOrder(BookingDetails details)
            {
                var supplierPrice = details.Agreement.Price.NetTotal;
                await _supplierOrderService.Add(details.ReferenceCode, ServiceTypes.HTL, supplierPrice);
            }


            Task LogAppliedMarkups(BookingDetails details) => _markupLogger.Write(details.ReferenceCode, ServiceTypes.HTL, responseWithMarkup.AppliedPolicies);


            async Task AuthorizeMoneyFromAccount(BookingDetails details)
            {
                var (_, isAuthorizeFailure, authorizeError) = await Result.Ok()
                    .OnSuccess(GetBooking)
                    .OnSuccessWithTransaction(_context, booking =>
                        Authorize(booking)
                            .OnSuccess(() => ChangePaymentStatusToAuthorized(booking)));

                // TODO: notify if fails
                if (isAuthorizeFailure)
                    _logger.LogDebug($"Could not authorize money: {authorizeError}");


                async Task<Booking> GetBooking()
                {
                    var entity = await _context.Bookings.FirstAsync(b => b.ReferenceCode == details.ReferenceCode);
                    // Booking was created in current instance of DbContext, so we need to detach it to change status
                    _context.Detach(entity);
                    return entity;
                }


                Task<Result> Authorize(Booking booking) => _paymentService.AuthorizeMoneyFromAccount(booking, customerInfo);


                async Task ChangePaymentStatusToAuthorized(Booking booking)
                {
                    if (booking.PaymentStatus == BookingPaymentStatuses.Authorized)
                        return;

                    booking.PaymentStatus = BookingPaymentStatuses.Authorized;
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
            }
        }


        public async Task<Result<BookingDetails, ProblemDetails>> ModifyBooking(int bookingId, BookingModifiedData modifiedData, string language)
        {
            return await ModifyBooking();

            async Task<Result<BookingDetails, ProblemDetails>> ModifyBooking()
            {
                return await _accommodationBookingManager.ModifyBooking(bookingId, modifiedData, language);
            } 
        }


        public async Task<Result<BookingAvailabilityInfo, ProblemDetails>> GetBookingAvailability(int availabilityId, Guid agreementId, string languageCode)
        {
            var availabilityResponse = await _availabilityResultsCache.Get(availabilityId);
            return await GetBookingAvailability(availabilityResponse, availabilityId, agreementId, languageCode);
        }


        public Task<Result<AccommodationBookingInfo>> GetBooking(int bookingId) => _accommodationBookingManager.Get(bookingId);


        public Task<Result<AccommodationBookingInfo>> GetBooking(string referenceCode) => _accommodationBookingManager.Get(referenceCode);


        public Task<Result<List<SlimAccommodationBookingInfo>>> GetCustomerBookings() => _accommodationBookingManager.GetForCurrentCustomer();


        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(int bookingId)
        {
            // TODO: implement money charge for cancel after deadline.
            return GetBooking()
                .OnSuccessWithTransaction(_context, booking =>
                    VoidMoney(booking)
                        .OnSuccess(() => _accommodationBookingManager.Cancel(bookingId))
                )
                .OnSuccess(CancelSupplierOrder);


            async Task<Result<Booking, ProblemDetails>> GetBooking()
            {
                var booking = await _context.Bookings
                    .SingleOrDefaultAsync(b => b.Id == bookingId);

                return booking is null
                    ? ProblemDetailsBuilder.Fail<Booking>($"Could not find booking with id '{bookingId}'")
                    : Result.Ok<Booking, ProblemDetails>(booking);
            }


            async Task<Result<VoidObject, ProblemDetails>> VoidMoney(Booking booking)
            {
                var (_, isFailure, error) = await _paymentService.VoidMoney(booking);

                return isFailure
                    ? ProblemDetailsBuilder.Fail<VoidObject>(error)
                    : Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);
            }


            async Task<VoidObject> CancelSupplierOrder(Booking booking)
            {
                var referenceCode = booking.ReferenceCode;
                await _supplierOrderService.Cancel(referenceCode);
                return VoidObject.Instance;
            }
        }


        private async Task<Result<BookingAvailabilityInfo, ProblemDetails>> GetBookingAvailability(
            SingleAccommodationAvailabilityDetailsWithMarkup responseWithMarkup,
            int availabilityId, Guid agreementId, string languageCode)
        {
            var availability = ExtractBookingAvailabilityInfo(responseWithMarkup.ResultResponse, agreementId);
            if (availability.Equals(default))
                return ProblemDetailsBuilder.Fail<BookingAvailabilityInfo>("Could not find availability by given id");

            var deadlineDetailsResponse = await _cancellationPoliciesService.GetDeadlineDetails(
                availabilityId.ToString(),
                availability.AccommodationId,
                availability.Agreement.TariffCode,
                DataProviders.Netstorming,
                languageCode);

            if (deadlineDetailsResponse.IsFailure)
                return ProblemDetailsBuilder.Fail<BookingAvailabilityInfo>($"Could not get deadline policies: {deadlineDetailsResponse.Error.Detail}");

            return Result.Ok<BookingAvailabilityInfo, ProblemDetails>(availability.AddDeadlineDetails(deadlineDetailsResponse.Value));
        }


        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(SingleAccommodationAvailabilityDetails response, Guid agreementId)
        {
            if (response.Equals(default))
                return default;

            return (from agreement in response.Agreements
                    where agreement.Id == agreementId
                    select new BookingAvailabilityInfo(
                        response.AccommodationDetails.Id,
                        response.AccommodationDetails.Name,
                        agreement,
                        response.AccommodationDetails.Location.LocalityCode,
                        response.AccommodationDetails.Location.Locality,
                        response.AccommodationDetails.Location.CountryCode,
                        response.AccommodationDetails.Location.Country,
                        response.CheckInDate,
                        response.CheckOutDate))
                .SingleOrDefault();
        }


        private readonly IAccommodationBookingManager _accommodationBookingManager;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly ICancellationPoliciesService _cancellationPoliciesService;
        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;


        private readonly IDataProviderClient _dataProviderClient;
        private readonly IMemoryFlow _flow;
        private readonly ILocationService _locationService;
        private readonly ILogger<AccommodationService> _logger;
        private readonly IMarkupLogger _markupLogger;
        private readonly IAvailabilityMarkupService _markupService;
        private readonly DataProviderOptions _options;
        private readonly IPaymentService _paymentService;
        private readonly IPermissionChecker _permissionChecker;
        private readonly ISupplierOrderService _supplierOrderService;
    }
}