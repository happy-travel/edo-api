using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.DataProviders;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    internal class AccommodationBookingManager : IAccommodationBookingManager
    {
        public AccommodationBookingManager(IDataProviderFactory dataProviderFactory,
            EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ICustomerContext customerContext,
            IServiceAccountContext serviceAccountContext,
            ITagProcessor tagProcessor)
        {
            _dataProviderFactory = dataProviderFactory;
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _customerContext = customerContext;
            _serviceAccountContext = serviceAccountContext;
            _tagProcessor = tagProcessor;
        }


        public async Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest bookingRequest, BookingAvailabilityInfo availabilityInfo,
            string languageCode)
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();

            if (isFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(error);

            var tags = await GetTags();
            return await ExecuteBookingRequest()
                .OnSuccess(SaveBookingResult);


            Task<Result<BookingDetails, ProblemDetails>> ExecuteBookingRequest()
            {
                // TODO: will be implemented in NIJO-31 
                var features = new List<Feature>(); //bookingRequest.Features

                var roomDetails = bookingRequest.RoomDetails
                    .Select(d => new SlimRoomDetails(d.Type, d.Passengers, d.IsExtraBedNeeded))
                    .ToList();

                var innerRequest = new BookingRequest(bookingRequest.AvailabilityId,
                    bookingRequest.AgreementId,
                    bookingRequest.Nationality,
                    PaymentMethods.BankTransfer,
                    tags.referenceCode,
                    bookingRequest.Residency,
                    roomDetails,
                    features,
                    bookingRequest.RejectIfUnavailable);

                // TODO: Replace this with conditional data provider support
                var dataProvider = _dataProviderFactory.Get(DataProviders.Netstorming);
                return dataProvider.Book(innerRequest, languageCode);
            }


            Task SaveBookingResult(BookingDetails confirmedBooking)
            {
                var booking = new AccommodationBookingBuilder()
                    .AddCustomerInfo(customerInfo)
                    .AddTags(tags.itn, tags.referenceCode)
                    .AddRequestInfo(bookingRequest)
                    .AddConfirmationDetails(confirmedBooking)
                    .AddServiceDetails(availabilityInfo)
                    .AddCreationDate(_dateTimeProvider.UtcNow())
                    .Build();

                _context.Bookings.Add(booking);
                return _context.SaveChangesAsync();
            }


            async Task<(string itn, string referenceCode)> GetTags()
            {
                string itn;
                if (string.IsNullOrWhiteSpace(bookingRequest.ItineraryNumber))
                {
                    itn = await _tagProcessor.GenerateItn();
                }
                else
                {
                    // User can send reference code instead of itn
                    if (!_tagProcessor.TryGetItnFromReferenceCode(bookingRequest.ItineraryNumber, out itn))
                        itn = bookingRequest.ItineraryNumber;

                    if (!await AreExistBookingsForItn(itn, customerInfo.CompanyId))
                        itn = await _tagProcessor.GenerateItn();
                }

                var referenceCode = await _tagProcessor.GenerateReferenceCode(ServiceTypes.HTL,
                    availabilityInfo.CountryCode,
                    itn);

                return (itn, referenceCode);
            }
        }


        public Task<Result<AccommodationBookingInfo>> Get(int bookingId) => GetCustomerBooking(booking => booking.Id == bookingId);


        public Task<Result<AccommodationBookingInfo>> Get(string referenceCode) => GetCustomerBooking(booking => booking.ReferenceCode == referenceCode);


        public async Task<Result<List<SlimAccommodationBookingInfo>>> GetForCurrentCustomer()
        {
            var (_, isFailure, customerData, error) = await _customerContext.GetCustomerInfo();

            if (isFailure)
                return Result.Fail<List<SlimAccommodationBookingInfo>>(error);

            var bookingData = await _context.Bookings
                .Where(b => b.CustomerId == customerData.CustomerId)
                .Select(b =>
                    new SlimAccommodationBookingInfo(b)
                ).ToListAsync();

            return Result.Ok(bookingData);
        }


        public async Task<Result<Booking, ProblemDetails>> Cancel(int bookingId)
        {
            var (_, isFailure, user, error) = await GetUserInfo();
            if (isFailure)
                return ProblemDetailsBuilder.Fail<Booking>(error);

            var booking = await _context.Bookings
                .SingleOrDefaultAsync(b => b.Id == bookingId && (user.Type == UserTypes.ServiceAccount || b.CustomerId == user.Id));

            if (booking is null)
                return ProblemDetailsBuilder.Fail<Booking>($"Could not find booking with ID '{bookingId}'");

            if (booking.Status == BookingStatusCodes.Cancelled)
                return ProblemDetailsBuilder.Fail<Booking>("Booking was already cancelled");

            return await ExecuteBookingCancel()
                .OnSuccess(async voidObj => await ChangeBookingToCancelled(booking));


            Task<Result<VoidObject, ProblemDetails>> ExecuteBookingCancel()
            {
                // TODO: Replace this with conditional data provider support
                var dataProvider = _dataProviderFactory.Get(DataProviders.Netstorming);
                return dataProvider.CancelBooking(booking.ReferenceCode);
            }


            async Task<Booking> ChangeBookingToCancelled(Booking bookingToCancel)
            {
                bookingToCancel.Status = BookingStatusCodes.Cancelled;
                if (booking.PaymentStatus == BookingPaymentStatuses.Authorized)
                    booking.PaymentStatus = BookingPaymentStatuses.Voided;
                if (booking.PaymentStatus == BookingPaymentStatuses.Captured)
                    booking.PaymentStatus = BookingPaymentStatuses.Refunded;

                var currentDetails = JsonConvert.DeserializeObject<AccommodationBookingDetails>(bookingToCancel.BookingDetails);
                bookingToCancel.BookingDetails = JsonConvert.SerializeObject(new AccommodationBookingDetails(currentDetails, BookingStatusCodes.Cancelled));

                _context.Update(bookingToCancel);
                await _context.SaveChangesAsync();
                return bookingToCancel;
            }


            Task<Result<UserInfo>> GetUserInfo()
                => _serviceAccountContext.GetUserInfo()
                    .OnFailureCompensate(_customerContext.GetUserInfo);
        }


        private async Task<Result<AccommodationBookingInfo>> GetCustomerBooking(Expression<Func<Booking, bool>> filterExpression)
        {
            var (_, isFailure, customerData, error) = await _customerContext.GetCustomerInfo();

            if (isFailure)
                return Result.Fail<AccommodationBookingInfo>(error);

            var bookingData = await _context.Bookings
                .Where(b => b.CustomerId == customerData.CustomerId)
                .Where(filterExpression)
                .Select(b => new AccommodationBookingInfo(b.Id,
                    JsonConvert.DeserializeObject<AccommodationBookingDetails>(b.BookingDetails),
                    JsonConvert.DeserializeObject<BookingAvailabilityInfo>(b.ServiceDetails),
                    b.CompanyId,
                    b.PaymentStatus)).FirstOrDefaultAsync();

            return bookingData.Equals(default)
                ? Result.Fail<AccommodationBookingInfo>("Could not get a booking data")
                : Result.Ok(bookingData);
        }


        // TODO: Replace method when will be added other services 
        private Task<bool> AreExistBookingsForItn(string itn, int customerId)
            => _context.Bookings.Where(b => b.CustomerId == customerId && b.ItineraryNumber == itn).AnyAsync();


        private readonly IDataProviderFactory _dataProviderFactory;
        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IServiceAccountContext _serviceAccountContext;
        private readonly ITagProcessor _tagProcessor;
    }
}