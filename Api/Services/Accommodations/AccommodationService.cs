﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Payments;
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
using HappyTravel.Edo.Data.Payments;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
            IAccountManagementService accountManagementService,
            EdoContext context,
            IPaymentProcessingService paymentProcessingService,
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
            _accountManagementService = accountManagementService;
            _context = context;
            _paymentProcessingService = paymentProcessingService;
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

            var (_, permissionDenied, permissionError) = await _permissionChecker.CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationAvailabilitySearch);
            if(permissionDenied)
                return ProblemDetailsBuilder.Fail<AvailabilityDetails>(permissionError);

            return await ExecuteRequest()
                .OnSuccess(ApplyMarkup)
                .OnSuccess(SaveToCache)
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


            Task<AvailabilityDetailsWithMarkup> ApplyMarkup(AvailabilityDetails response) 
                => _markupService.Apply(customerInfo, response);


            Task SaveToCache(AvailabilityDetailsWithMarkup response) => _availabilityResultsCache.Set(response);


            AvailabilityDetails ReturnResponseWithMarkup(AvailabilityDetailsWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request, string languageCode)
        {
            // TODO: Refactor and simplify method
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if(isCustomerFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(customerError);

            var (_, permissionDenied, permissionError) = await _permissionChecker
                .CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationBooking);
            
            if (permissionDenied)
                return ProblemDetailsBuilder.Fail<BookingDetails>(permissionError);

            var responseWithMarkup = await _availabilityResultsCache.Get(request.AvailabilityId);
            var (_, isFailure, bookingAvailability, error) = await GetBookingAvailability(responseWithMarkup, request.AvailabilityId, request.AgreementId, languageCode);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(error.Detail);

            return await Book()
                .OnSuccess(SaveSupplierOrder)
                .OnSuccess(LogAppliedMarkups)
                .OnSuccess(AuthorizeMoneyFromAccount);


            Task<Result<BookingDetails, ProblemDetails>> Book()
                => _accommodationBookingManager.Book(request, bookingAvailability, languageCode);


            async Task SaveSupplierOrder(BookingDetails details)
            {
                var supplierPrice = details.Agreement.Price.NetTotal;
                await _supplierOrderService.Add(details.ReferenceCode, ServiceTypes.HTL, supplierPrice);
            }


            Task LogAppliedMarkups(BookingDetails details)
                => _markupLogger.Write(details.ReferenceCode, ServiceTypes.HTL, responseWithMarkup.AppliedPolicies);


            async Task AuthorizeMoneyFromAccount(BookingDetails details)
            {
                var (_, isAuthorizeFailure, authorizeError) = await Result.Ok()
                    .Ensure(CanAuthorize, "Money cannot be authorized for accommodation")
                    .OnSuccess(GetAccountAndUser)
                    .OnSuccessWithTransaction(_context, accountAndUser =>
                        AuthorizeMoney(accountAndUser.account, accountAndUser.user)
                            .OnSuccess(ChangePaymentStatusToAuthorized)
                    );

                // TODO: notify if fails
                if (isAuthorizeFailure)
                    _logger.LogDebug($"Could not authorize money: {authorizeError}");


                bool CanAuthorize() => request.PaymentMethod == PaymentMethods.BankTransfer && BookingStatusesForFreeze.Contains(details.Status);


                async Task<Result<(PaymentAccount account, UserInfo user)>> GetAccountAndUser()
                {
                    var (_, isUserFailure, user, userError) = await _customerContext.GetUserInfo();
                    if (isUserFailure)
                        return Result.Fail<(PaymentAccount, UserInfo)>(userError);

                    if (!Enum.TryParse<Currencies>(details.Agreement.Price.CurrencyCode, out var currency))
                        return Result.Fail<(PaymentAccount, UserInfo)>(
                            $"Unsupported currency in agreement: {details.Agreement.Price.CurrencyCode}");

                    var result = await _accountManagementService.Get(customerInfo.CompanyId, currency);
                    return result.Map(account => (account, user));
                }


                async Task<Result> AuthorizeMoney(PaymentAccount account, UserInfo userInfo)
                {
                    var price = (await _availabilityResultsCache.Get(request.AvailabilityId))
                        .ResultResponse
                        .Results
                        .SelectMany(r => r.Agreements)
                        .Single(a => a.Id == request.AgreementId)
                        .Price
                        .NetTotal;
                    
                    return await _paymentProcessingService.AuthorizeMoney(account.Id, new AuthorizedMoneyData(
                            currency: account.Currency,
                            amount: price,
                            reason: $"Authorize money after booking '{details.ReferenceCode}'",
                            referenceCode: details.ReferenceCode),
                        userInfo);
                }


                async Task ChangePaymentStatusToAuthorized()
                {
                    var booking = await _context.Bookings.FirstAsync(b => b.ReferenceCode == details.ReferenceCode);
                    // Booking was created in current instance of DbContext, so we need to detach it to change status
                    _context.Detach(booking);

                    if (booking.PaymentStatus == BookingPaymentStatuses.Authorized)
                        return;

                    booking.PaymentStatus = BookingPaymentStatuses.Authorized;
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
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
                // TODO: Implement refund money if status is paid with deadline penalty?
                // TODO: Implement capture and void money from cards
                if (booking.PaymentStatus != BookingPaymentStatuses.Authorized || booking.PaymentMethod != PaymentMethods.BankTransfer)
                    return Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);

                var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

                var (_, isFailure, error) = await GetCustomer()
                    .OnSuccess(GetAccount)
                    .OnSuccess(VoidMoneyFromAccount);

                return isFailure
                    ? ProblemDetailsBuilder.Fail<VoidObject>(error)
                    : Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);

                async Task<Result<CustomerInfo>> GetCustomer() => await _customerContext.GetCustomerInfo();


                Task<Result<PaymentAccount>> GetAccount(CustomerInfo customerInfo)
                    => Enum.TryParse<Currencies>(bookingAvailability.Agreement.Price.CurrencyCode, out var currency)
                        ? _accountManagementService.Get(customerInfo.CompanyId, currency)
                        : Task.FromResult(Result.Fail<PaymentAccount>($"Unsupported currency in agreement: {bookingAvailability.Agreement.Price.CurrencyCode}"));


                Task<Result> VoidMoneyFromAccount(PaymentAccount account)
                {
                    return GetUser()
                        .OnSuccess(userInfo =>
                            _paymentProcessingService.VoidMoney(account.Id, new AuthorizedMoneyData(bookingAvailability.Agreement.Price.NetTotal,
                                account.Currency, reason: $"Void money after booking cancellation '{booking.ReferenceCode}'",
                                referenceCode: booking.ReferenceCode), userInfo));

                    Task<Result<UserInfo>> GetUser() => _customerContext.GetUserInfo();
                }
            }


            async Task<VoidObject> CancelSupplierOrder(Booking booking)
            {
                var referenceCode = booking.ReferenceCode;
                await _supplierOrderService.Cancel(referenceCode);
                return VoidObject.Instance;
            }
        }


        private async Task<Result<BookingAvailabilityInfo, ProblemDetails>> GetBookingAvailability(AvailabilityDetailsWithMarkup responseWithMarkup,
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
        

        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(AvailabilityDetails response, Guid agreementId)
        {
            if (response.Equals(default))
                return default;

            return (from availabilityResult in response.Results
                    from agreement in availabilityResult.Agreements
                    where agreement.Id == agreementId
                    select new BookingAvailabilityInfo(
                        availabilityResult.AccommodationDetails.Id,
                        availabilityResult.AccommodationDetails.Name,
                        agreement,
                        availabilityResult.AccommodationDetails.Location.CityCode,
                        availabilityResult.AccommodationDetails.Location.City,
                        availabilityResult.AccommodationDetails.Location.CountryCode,
                        availabilityResult.AccommodationDetails.Location.Country,
                        response.CheckInDate,
                        response.CheckOutDate))
                .SingleOrDefault();
        }


        private static readonly HashSet<BookingStatusCodes> BookingStatusesForFreeze = new HashSet<BookingStatusCodes>
        {
            BookingStatusCodes.Pending, BookingStatusCodes.Confirmed
        };

        private readonly IAccommodationBookingManager _accommodationBookingManager;
        private readonly IAccountManagementService _accountManagementService;
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
        private readonly IPaymentProcessingService _paymentProcessingService;
        private readonly ISupplierOrderService _supplierOrderService;
        private readonly IPermissionChecker _permissionChecker;
    }
}
