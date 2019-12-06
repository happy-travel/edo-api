using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.CodeGeneration;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentMethods = HappyTravel.EdoContracts.General.Enums.PaymentMethods;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    internal class AccommodationBookingManager : IAccommodationBookingManager
    {
        public AccommodationBookingManager(IOptions<DataProviderOptions> options,
            IDataProviderClient dataProviderClient,
            EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ICustomerContext customerContext,
            ITagGenerator tagGenerator)
        {
            _dataProviderClient = dataProviderClient;
            _options = options.Value;
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _customerContext = customerContext;
            _tagGenerator = tagGenerator;
        }


        public async Task<Result<BookingDetails, ProblemDetails>> Book(AccommodationBookingRequest bookingRequest, BookingAvailabilityInfo availabilityInfo,
            string languageCode)
        {
            var (_, isFailure, customerInfo, error) = await _customerContext.GetCustomerInfo();

            if (isFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(error);

            var itn = !string.IsNullOrWhiteSpace(bookingRequest.ItineraryNumber)
                ? bookingRequest.ItineraryNumber
                : await _tagGenerator.GenerateItn();

            var referenceCode = await _tagGenerator.GenerateReferenceCode(ServiceTypes.HTL,
                availabilityInfo.CountryCode,
                itn);

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
                    referenceCode,
                    bookingRequest.Residency,
                    roomDetails,
                    features,
                    bookingRequest.RejectIfUnavailable);

                return _dataProviderClient.Post<BookingRequest, BookingDetails>(
                    new Uri(_options.Netstorming + "bookings/accommodations", UriKind.Absolute),
                    innerRequest, languageCode);
            }


            Task SaveBookingResult(BookingDetails confirmedBooking)
            {
                var booking = new AccommodationBookingBuilder()
                    .AddCustomerInfo(customerInfo)
                    .AddTags(itn, referenceCode)
                    .AddRequestInfo(bookingRequest)
                    .AddConfirmationDetails(confirmedBooking)
                    .AddServiceDetails(availabilityInfo)
                    .AddCreationDate(_dateTimeProvider.UtcNow())
                    .Build();

                _context.Bookings.Add(booking);
                return _context.SaveChangesAsync();
            }
        }


        public Task<Result<AccommodationBookingInfo>> Get(int bookingId) => GetCustomerBooking(booking => booking.Id == bookingId);


        public Task<Result<AccommodationBookingInfo>> Get(string referenceCode) => GetCustomerBooking(booking => booking.ReferenceCode == referenceCode);


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
                    b.CompanyId)).FirstOrDefaultAsync();

            return bookingData.Equals(default)
                ? Result.Fail<AccommodationBookingInfo>("Could not get a booking data")
                : Result.Ok(bookingData);
        }


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
            var (_, isFailure, customerData, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return ProblemDetailsBuilder.Fail<Booking>(error);

            var booking = await _context.Bookings
                .SingleOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerData.CustomerId);

            if (booking is null)
                return ProblemDetailsBuilder.Fail<Booking>($"Could not find booking with ID '{bookingId}'");

            if (booking.Status == BookingStatusCodes.Cancelled)
                return ProblemDetailsBuilder.Fail<Booking>("Booking was already cancelled");

            return await ExecuteBookingCancel()
                .OnSuccess(async voidObj => await ChangeBookingToCancelled(booking));


            Task<Result<VoidObject, ProblemDetails>> ExecuteBookingCancel()
                => _dataProviderClient.Post(new Uri(_options.Netstorming + "bookings/accommodations/" + booking.ReferenceCode + "/cancel",
                    UriKind.Absolute));


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
        }


        public async Task<Result<BookingDetails, ProblemDetails>> ModifyBooking(int bookingId, BookingModifiedData modifiedData, string languageCode)
        {
            var bookingDataResult = await Get(bookingId);
            if (bookingDataResult.IsFailure)
                return ProblemDetailsBuilder.Fail<BookingDetails>(bookingDataResult.Error);

            var bookingData = bookingDataResult.Value;
            var bookingDetails = bookingData.BookingDetails;
            var serviceDetails = bookingData.ServiceDetails;

            var roomDetails = modifiedData.RoomDetails
                .Select(d => new SlimRoomDetails(d.Type, d.Passengers, d.IsExtraBedNeeded))
                .ToList();
            var features = new List<Feature>();

            var availabilityId = serviceDetails.DeadlineDetails.AvailabilityId;

            var modifyRequest = new ModifyBookingRequest(
                modifiedData.CheckInDate,
                modifiedData.CheckOutDate,
                serviceDetails.Agreement.Rooms,
                modifiedData.Features,
                new BookingRequest(availabilityId,
                    serviceDetails.Agreement.Id,
                    modifiedData.Nationality,
                    PaymentMethods.BankTransfer,
                    bookingDetails.ReferenceCode,
                    modifiedData.Residency,
                    roomDetails,
                    features,
                    modifiedData.RejectIfUnavailable),
                new SearchInfo(availabilityId,
                    bookingDetails.TariffCode,
                    serviceDetails.AccommodationId)
            );


            return await _dataProviderClient.Post<ModifyBookingRequest, BookingDetails>(
                new Uri( _options.Netstorming + "bookings/accommodations/modify", UriKind.Absolute),
                modifyRequest, languageCode);
        }


        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;
        private readonly IDataProviderClient _dataProviderClient;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly DataProviderOptions _options;
        private readonly ITagGenerator _tagGenerator;
    }
}