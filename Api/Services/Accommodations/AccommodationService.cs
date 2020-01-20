using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using FluentValidation;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Markups.Availability;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Deadline;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Mailing;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Availability;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using AvailabilityRequest = HappyTravel.Edo.Api.Models.Availabilities.AvailabilityRequest;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AccommodationService : IAccommodationService
    {
        public AccommodationService(IMemoryFlow flow,
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
            ILogger<AccommodationService> logger,
            IServiceAccountContext serviceAccountContext,
            IDateTimeProvider dateTimeProvider,
            IBookingMailingService bookingMailingService,
            IDataProviderFactory dataProviderFactory,
            IMultiProviderAvailabilityManager availabilityManager)
        {
            _flow = flow;
            _locationService = locationService;
            _accommodationBookingManager = accommodationBookingManager;
            _availabilityResultsCache = availabilityResultsCache;
            _customerContext = customerContext;
            _markupService = markupService;
            _cancellationPoliciesService = cancellationPoliciesService;
            _supplierOrderService = supplierOrderService;
            _markupLogger = markupLogger;
            _permissionChecker = permissionChecker;
            _context = context;
            _paymentService = paymentService;
            _logger = logger;
            _serviceAccountContext = serviceAccountContext;
            _dateTimeProvider = dateTimeProvider;
            _bookingMailingService = bookingMailingService;
            _dataProviderFactory = dataProviderFactory;
            _availabilityManager = availabilityManager;
        }


        public ValueTask<Result<AccommodationDetails, ProblemDetails>> Get(string accommodationId, string languageCode)
        {
            // TODO: replace with conditional data provider
            var dataProvider = _dataProviderFactory.Get(DataProviders.Netstorming);
            return _flow.GetOrSetAsync(_flow.BuildKey(nameof(AccommodationService), "Accommodations", languageCode, accommodationId),
                async () => await dataProvider.GetAccommodation(accommodationId, languageCode),
                TimeSpan.FromDays(1));
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

            var (_, isAvailabilityFailure, responseWithMarkup, availabilityError) = await _availabilityResultsCache.Get(request.AvailabilityId);
            if (isAvailabilityFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(availabilityError);

            var (_, isFailure, bookingAvailability, error) =
                await GetBookingAvailability(responseWithMarkup, request.AvailabilityId, request.AgreementId, languageCode);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(error.Detail);

            return await Book()
                .OnSuccess(SaveSupplierOrder)
                .OnSuccess(LogAppliedMarkups);

            Task<Result<BookingDetails, ProblemDetails>> Book() => _accommodationBookingManager.Book(request, bookingAvailability, languageCode);


            async Task SaveSupplierOrder(BookingDetails details)
            {
                var supplierPrice = details.Agreement.Price.NetTotal;
                await _supplierOrderService.Add(details.ReferenceCode, ServiceTypes.HTL, supplierPrice);
            }


            Task LogAppliedMarkups(BookingDetails details) => _markupLogger.Write(details.ReferenceCode, ServiceTypes.HTL, responseWithMarkup.AppliedPolicies);
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
                .OnSuccess(NotifyCustomer)
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


            async Task NotifyCustomer(Booking booking)
            {
                var customer = await _context.Customers.SingleOrDefaultAsync(c => c.Id == booking.CustomerId);
                if (customer == default)
                {
                    _logger.LogWarning("Booking cancellation notification: could not find customer with id '{0}' for booking '{1}'",
                        booking.CustomerId, booking.ReferenceCode);
                    return;
                }

                await _bookingMailingService.NotifyBookingCancelled(booking.ReferenceCode, customer.Email, $"{customer.LastName} {customer.FirstName}");
            }
        }


        public async Task<Result<List<int>>> GetBookingsForCancellation(DateTime deadlineDate)
        {
            if (deadlineDate == default)
                return Result.Fail<List<int>>("Deadline date should be specified");

            var (_, isFailure, _, error) = await _serviceAccountContext.GetUserInfo();
            if (isFailure)
                return Result.Fail<List<int>>(error);

            // It’s prohibited to cancel booking after check-in date
            var currentDateUtc = _dateTimeProvider.UtcNow();
            var bookings = await _context.Bookings
                .Where(booking =>
                    BookingStatusesForCancellation.Contains(booking.Status) &&
                    booking.PaymentStatus == BookingPaymentStatuses.NotPaid &&
                    booking.BookingDate > currentDateUtc)
                .ToListAsync();

            var dayBeforeDeadline = deadlineDate.Date.AddDays(1);
            var bookingIds = bookings
                .Where(booking =>
                {
                    var availabilityInfo = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);
                    return availabilityInfo.Agreement.DeadlineDate.Date <= dayBeforeDeadline;
                })
                .Select(booking => booking.Id)
                .ToList();

            return Result.Ok(bookingIds);
        }


        public async Task<Result<ProcessResult>> CancelBookings(List<int> bookingIds)
        {
            var (_, isUserFailure, _, userError) = await _serviceAccountContext.GetUserInfo();
            if (isUserFailure)
                return Result.Fail<ProcessResult>(userError);

            var bookings = await GetBookings();

            return await Validate()
                .OnSuccess(ProcessBookings);


            Task<List<Booking>> GetBookings()
            {
                var ids = bookingIds;
                return _context.Bookings.Where(booking => ids.Contains(booking.Id)).ToListAsync();
            }


            Result Validate()
            {
                return bookings.Count != bookingIds.Count
                    ? Result.Fail("Invalid booking ids. Could not find some of requested bookings.")
                    : Result.Combine(bookings.Select(CheckCanBeCancelled).ToArray());


                Result CheckCanBeCancelled(Booking booking)
                    => GenericValidator<Booking>.Validate(v =>
                    {
                        v.RuleFor(c => c.PaymentStatus)
                            .Must(status => booking.PaymentStatus == BookingPaymentStatuses.NotPaid)
                            .WithMessage(
                                $"Invalid payment status for booking '{booking.ReferenceCode}': {booking.PaymentStatus}");
                        v.RuleFor(c => c.Status)
                            .Must(status => BookingStatusesForCancellation.Contains(status))
                            .WithMessage($"Invalid booking status for booking '{booking.ReferenceCode}': {booking.Status}");
                    }, booking);
            }


            Task<ProcessResult> ProcessBookings()
            {
                return Combine(bookings.Select(ProcessBooking));


                Task<Result<string>> ProcessBooking(Booking booking)
                {
                    return CancelBooking(booking.Id)
                        .OnBoth(CreateResult);


                    Result<string> CreateResult(Result<VoidObject, ProblemDetails> result)
                        => result.IsSuccess
                            ? Result.Ok($"Booking '{booking.ReferenceCode}' was cancelled.")
                            : Result.Fail<string>($"Unable to cancel booking '{booking.ReferenceCode}'. Reason: {result.Error.Detail}");
                }


                async Task<ProcessResult> Combine(IEnumerable<Task<Result<string>>> results)
                {
                    var builder = new StringBuilder();

                    foreach (var result in results)
                    {
                        var (_, isFailure, value, error) = await result;
                        builder.AppendLine(isFailure ? error : value);
                    }

                    return new ProcessResult(builder.ToString());
                }
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


        private static readonly HashSet<BookingStatusCodes> BookingStatusesForCancellation = new HashSet<BookingStatusCodes>
        {
            BookingStatusCodes.Pending, BookingStatusCodes.Confirmed
        };


        private readonly IAccommodationBookingManager _accommodationBookingManager;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly IBookingMailingService _bookingMailingService;
        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly IMultiProviderAvailabilityManager _availabilityManager;
        private readonly ICancellationPoliciesService _cancellationPoliciesService;
        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;


        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMemoryFlow _flow;
        private readonly ILocationService _locationService;
        private readonly ILogger<AccommodationService> _logger;
        private readonly IMarkupLogger _markupLogger;
        private readonly IAvailabilityMarkupService _markupService;
        private readonly IPaymentService _paymentService;
        private readonly IPermissionChecker _permissionChecker;
        private readonly IServiceAccountContext _serviceAccountContext;
        private readonly ISupplierOrderService _supplierOrderService;
    }
}