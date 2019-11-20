using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Availabilities;
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
using HappyTravel.Edo.Data.Infrastructure.DatabaseExtensions;
using HappyTravel.Edo.Data.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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


        public ValueTask<Result<RichAccommodationDetails, ProblemDetails>> Get(string accommodationId, string languageCode)
            => _flow.GetOrSetAsync(_flow.BuildKey(nameof(AccommodationService), "Accommodations", languageCode, accommodationId),
                async () => await _dataProviderClient.Get<RichAccommodationDetails>(
                    new Uri($"{_options.Netstorming}hotels/{accommodationId}", UriKind.Absolute), languageCode),
                TimeSpan.FromDays(1));


        public async ValueTask<Result<AvailabilityResponse, ProblemDetails>> GetAvailable(AvailabilityRequest request, string languageCode)
        {
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return ProblemDetailsBuilder.Fail<AvailabilityResponse>(customerError);

            var (_, permissionDenied, permissionError) = await _permissionChecker.CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationAvailabilitySearch);
            if(permissionDenied)
                return ProblemDetailsBuilder.Fail<AvailabilityResponse>(permissionError);
            
            var (_, isFailure, location, error) = await _locationService.Get(request.Location, languageCode);
            if (isFailure)
                return Result.Fail<AvailabilityResponse, ProblemDetails>(error);

            return await ExecuteRequest()
                .OnSuccess(ApplyMarkup)
                .OnSuccess(SaveToCache)
                .OnSuccess(ReturnResponseWithMarkup);


            Task<Result<AvailabilityResponse, ProblemDetails>> ExecuteRequest()
                => _dataProviderClient.Post<InnerAvailabilityRequest, AvailabilityResponse>(
                    new Uri(_options.Netstorming + "hotels/availability", UriKind.Absolute),
                    new InnerAvailabilityRequest(request, location), languageCode);


            Task<AvailabilityResponseWithMarkup> ApplyMarkup(AvailabilityResponse response) => _markupService.Apply(customerInfo, response);

            Task SaveToCache(AvailabilityResponseWithMarkup response) => _availabilityResultsCache.Set(response);

            AvailabilityResponse ReturnResponseWithMarkup(AvailabilityResponseWithMarkup markup) => markup.ResultResponse;
        }


        public async Task<Result<AccommodationBookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request, string languageCode)
        {
            // TODO: Refactor and simplify method
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();
            if(isCustomerFailure)
                return ProblemDetailsBuilder.Fail<AccommodationBookingDetails>(customerError);

            var (_, permissionDenied, permissionError) = await _permissionChecker
                    .CheckInCompanyPermission(customerInfo, InCompanyPermissions.AccommodationBooking);
            
            if (permissionDenied)
                return ProblemDetailsBuilder.Fail<AccommodationBookingDetails>(permissionError); 
            
            var responseWithMarkup = await _availabilityResultsCache.Get(request.AvailabilityId);
            var (_, isFailure, bookingAvailability, error) =
                await GetBookingAvailability(responseWithMarkup, request.AvailabilityId, request.AgreementId, languageCode);
            if (isFailure)
                return ProblemDetailsBuilder.Fail<AccommodationBookingDetails>(error.Detail);

            return await Book()
                .OnSuccess(SaveSupplierOrder)
                .OnSuccess(LogAppliedMarkups)
                .OnSuccess(FreezeMoneyFromAccount);


            Task<Result<AccommodationBookingDetails, ProblemDetails>> Book()
                => _accommodationBookingManager.Book(
                    request,
                    bookingAvailability,
                    languageCode);


            async Task<AccommodationBookingDetails> SaveSupplierOrder(AccommodationBookingDetails details)
            {
                var supplierAvailability = ExtractBookingAvailabilityInfo(responseWithMarkup.SupplierResponse, request.AgreementId);
                var supplierPrice = supplierAvailability.Agreement.Price.Total;
                await _supplierOrderService.Add(details.ReferenceCode, ServiceTypes.HTL, supplierPrice);
                return details;
            }


            Task LogAppliedMarkups(AccommodationBookingDetails details)
                => _markupLogger.Write(details.ReferenceCode, ServiceTypes.HTL, responseWithMarkup.AppliedPolicies);


            async Task FreezeMoneyFromAccount(AccommodationBookingDetails details)
            {
                var (_, isFreezeFailure, freezeError) = await Result.Ok()
                    .Ensure(CanFreeze, "Money cannot be frozen for accommodation")
                    .OnSuccess(GetAccountAndUser)
                    .OnSuccessWithTransaction(_context, accountAndUser =>
                        FreezeMoney(accountAndUser.account, accountAndUser.user)
                            .OnSuccess(ChangePaymentStatusToFrozen)
                    );

                // TODO: notify if fails
                if (isFreezeFailure)
                    _logger.LogDebug($"Could not freeze money: {freezeError}");


                bool CanFreeze() => request.PaymentMethod == PaymentMethods.BankTransfer && BookingStatusesForFreeze.Contains(details.Status);


                async Task<Result<(PaymentAccount account, UserInfo user)>> GetAccountAndUser()
                {
                    var (_, isUserFailure, user, userError) = await _customerContext.GetUserInfo();
                    if (isUserFailure)
                        return Result.Fail<(PaymentAccount, UserInfo)>(userError);

                    if (!Enum.TryParse<Currencies>(bookingAvailability.Agreement.CurrencyCode, out var currency))
                        return Result.Fail<(PaymentAccount, UserInfo)>(
                            $"Invalid currency in details: {bookingAvailability.Agreement.CurrencyCode}");

                    var result = await _accountManagementService.Get(customerInfo.CompanyId, currency);
                    return result.Map(account => (account, user));
                }


                Task<Result> FreezeMoney(PaymentAccount account, UserInfo userInfo)
                    => _paymentProcessingService.FreezeMoney(account.Id, new FrozenMoneyData(
                            currency: account.Currency,
                            amount: bookingAvailability.Agreement.Price.Total,
                            reason: $"Freeze money after booking '{details.ReferenceCode}'",
                            referenceCode: details.ReferenceCode),
                        userInfo);


                async Task ChangePaymentStatusToFrozen()
                {
                    var booking = await _context.Bookings.FirstAsync(b => b.ReferenceCode == details.ReferenceCode);
                    // Booking was created in current instance of DbContext, so we need to detach it to change status
                    _context.Detach(booking);

                    if (booking.PaymentStatus == BookingPaymentStatuses.MoneyFrozen)
                        return;

                    booking.PaymentStatus = BookingPaymentStatuses.MoneyFrozen;
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


        public Task<List<AccommodationBookingInfo>> GetBookings() => _accommodationBookingManager.Get();


        public Task<Result<VoidObject, ProblemDetails>> CancelBooking(int bookingId)
        {
            // TODO: implement money charge for cancel after deadline.
            return GetBooking()
                .OnSuccessWithTransaction(_context, booking =>
                    UnfreezeMoney(booking)
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


            async Task<Result<VoidObject, ProblemDetails>> UnfreezeMoney(Booking booking)
            {
                // TODO: Need unfreeze money if status is paid?
                // TODO: Implement freeze and unfreeze money from cards
                if (booking.PaymentStatus != BookingPaymentStatuses.MoneyFrozen || booking.PaymentMethod != PaymentMethods.BankTransfer)
                    return Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);

                var bookingAvailability = JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails);

                var (_, isUnfreezeFailure, unfreezeError) = await GetCustomer()
                    .OnSuccess(GetAccount)
                    .OnSuccess(UnfreezeMoneyFromAccount);

                return isUnfreezeFailure
                    ? ProblemDetailsBuilder.Fail<VoidObject>(unfreezeError)
                    : Result.Ok<VoidObject, ProblemDetails>(VoidObject.Instance);

                async Task<Result<CustomerInfo>> GetCustomer() => await _customerContext.GetCustomerInfo();


                Task<Result<PaymentAccount>> GetAccount(CustomerInfo customerInfo)
                    => Enum.TryParse<Currencies>(bookingAvailability.Agreement.CurrencyCode, out var currency)
                        ? _accountManagementService.Get(customerInfo.CompanyId, currency)
                        : Task.FromResult(Result.Fail<PaymentAccount>($"Invalid currency in details: {bookingAvailability.Agreement.CurrencyCode}"));


                Task<Result> UnfreezeMoneyFromAccount(PaymentAccount account)
                {
                    return GetUser()
                        .OnSuccess(userInfo =>
                            _paymentProcessingService.UnfreezeMoney(account.Id, new FrozenMoneyData(bookingAvailability.Agreement.Price.Total,
                                account.Currency, reason: $"Unfreeze money after booking cancellation '{booking.ReferenceCode}'",
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


        private async Task<Result<BookingAvailabilityInfo, ProblemDetails>> GetBookingAvailability(AvailabilityResponseWithMarkup responseWithMarkup,
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


        private BookingAvailabilityInfo ExtractBookingAvailabilityInfo(AvailabilityResponse response, Guid agreementId)
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
