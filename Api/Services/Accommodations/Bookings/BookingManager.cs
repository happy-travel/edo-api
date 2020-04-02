using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.CodeProcessors;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    internal class BookingManager : IBookingManager
    {
        public BookingManager(EdoContext context,
            IDateTimeProvider dateTimeProvider,
            ICustomerContext customerContext,
            ITagProcessor tagProcessor,
            ILogger<BookingManager> logger)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
            _customerContext = customerContext;
            _tagProcessor = tagProcessor;
            _logger = logger;
        }


        public async Task<Result<string>> Register(AccommodationBookingRequest bookingRequest, BookingAvailabilityInfo availabilityInfo)
        {
            var (_, isCustomerFailure, customerInfo, customerError) = await _customerContext.GetCustomerInfo();

            return isCustomerFailure
                ? ProblemDetailsBuilder.Fail<string>(customerError)
                : Result.Ok(await CreateBooking());


            async Task<string> CreateBooking()
            {
                var tags = await GetTags();
                var initialBooking = new BookingBuilder()
                    .AddCreationDate(_dateTimeProvider.UtcNow())
                    .AddCustomerInfo(customerInfo)
                    .AddTags(tags.itn, tags.referenceCode)
                    .AddStatus(BookingStatusCodes.InternalProcessing)
                    .AddServiceDetails(availabilityInfo)
                    .AddPaymentMethod(bookingRequest.PaymentMethod)
                    .AddRequestInfo(bookingRequest)
                    .AddProviderInfo(bookingRequest.DataProvider)
                    .AddPaymentStatus(BookingPaymentStatuses.NotPaid)
                    .Build();

                _context.Bookings.Add(initialBooking);

                await _context.SaveChangesAsync();

                return tags.referenceCode;
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

                var referenceCode = await _tagProcessor.GenerateReferenceCode(
                    ServiceTypes.HTL,
                    availabilityInfo.CountryCode,
                    itn);

                return (itn, referenceCode);
            }
        }


        public async Task<BookingDetails> Finalize(
            Data.Booking.Booking booking,
            BookingDetails bookingDetails)
        {
            var bookingEntity = new BookingBuilder(booking)
                .AddBookingDetails(bookingDetails)
                .AddStatus(bookingDetails.Status)
                .Build();
            
            _context.Bookings.Update(bookingEntity);
            await _context.SaveChangesAsync();
            _context.Entry(bookingEntity).State = EntityState.Detached;
            return bookingDetails;
        }


        public async Task<Result> UpdateBookingDetails(BookingDetails bookingDetails, Data.Booking.Booking booking)
        {
            var previousBookingDetails = JsonConvert.DeserializeObject<BookingDetails>(booking.BookingDetails);
            booking.BookingDetails = JsonConvert.SerializeObject(new BookingDetails(bookingDetails, previousBookingDetails.RoomContractSet));
            booking.Status = bookingDetails.Status;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            return Result.Ok();
        }


        public Task<Result> ConfirmBooking(BookingDetails bookingDetails, Data.Booking.Booking booking)
        {
            booking.BookingDate = _dateTimeProvider.UtcNow();
            return UpdateBookingDetails(bookingDetails, booking);
        }


        public Task<Result> ConfirmBookingCancellation(BookingDetails bookingDetails, Data.Booking.Booking booking)
        {
            if (booking.PaymentStatus == BookingPaymentStatuses.Authorized || booking.PaymentStatus == BookingPaymentStatuses.PartiallyAuthorized)
                booking.PaymentStatus = BookingPaymentStatuses.Voided;
            if (booking.PaymentStatus == BookingPaymentStatuses.Captured)
                booking.PaymentStatus = BookingPaymentStatuses.Refunded;

            return UpdateBookingDetails(bookingDetails, booking);
        }


        public Task<Result<Data.Booking.Booking>> Get(string referenceCode)
        {
            return Get(booking => booking.ReferenceCode == referenceCode);
        }


        public Task<Result<Data.Booking.Booking>> Get(int bookingId)
        {
            return Get(booking => booking.Id == bookingId);
        }


        private async Task<Result<Data.Booking.Booking>> Get(Expression<Func<Data.Booking.Booking, bool>> filterExpression)
        {
            var booking = await _context.Bookings
                .Where(filterExpression)
                .SingleOrDefaultAsync();

            return booking == default
                ? Result.Fail<Data.Booking.Booking>("Could not get booking data")
                : Result.Ok(booking);
        }


        public async Task<Result<Data.Booking.Booking>> GetCustomersBooking(string referenceCode)
        {
            var (_, isCustomerFailure, customerData, customerError) = await _customerContext.GetCustomerInfo();
            if (isCustomerFailure)
                return Result.Fail<Data.Booking.Booking>(customerError);

            return await Get(booking => customerData.CustomerId == booking.CustomerId && booking.ReferenceCode == referenceCode);
        }


        public async Task<Result<AccommodationBookingInfo>> GetCustomerBookingInfo(int bookingId)
        {
            var customerData = await _customerContext.GetCustomer();

            var bookingDataResult = await Get(booking => customerData.CustomerId == booking.CustomerId && booking.Id == bookingId);
            if (bookingDataResult.IsFailure)
                return Result.Fail<AccommodationBookingInfo>(bookingDataResult.Error);

            return Result.Ok(ConvertToBookingInfo(bookingDataResult.Value));
        }


        public async Task<Result<AccommodationBookingInfo>> GetCustomerBookingInfo(string referenceCode)
        {
            var customerData = await _customerContext.GetCustomer();

            var bookingDataResult = await Get(booking => customerData.CustomerId == booking.CustomerId && booking.ReferenceCode == referenceCode);
            if (bookingDataResult.IsFailure)
                return Result.Fail<AccommodationBookingInfo>(bookingDataResult.Error);

            return Result.Ok(ConvertToBookingInfo(bookingDataResult.Value));
        }


        /// <summary>
        /// Gets all booking info of the current customer
        /// </summary>
        /// <returns>List of the slim booking models </returns>
        public async Task<Result<List<SlimAccommodationBookingInfo>>> GetCustomerBookingsInfo()
        {
            var customerData = await _customerContext.GetCustomer();

            var bookingData = await _context.Bookings
                .Where(b => b.CustomerId == customerData.CustomerId
                    && b.BookingDetails != null
                    && b.ServiceDetails != null)
                .Select(b =>
                    new SlimAccommodationBookingInfo(b)
                ).ToListAsync();

            return Result.Ok(bookingData);
        }


        private AccommodationBookingInfo ConvertToBookingInfo(Data.Booking.Booking booking)
        {
            var bookingDetails = !string.IsNullOrEmpty(booking.BookingDetails)
                ? GetDetails()
                : default;
            var serviceDetails = !string.IsNullOrEmpty(booking.ServiceDetails)
                ? JsonConvert.DeserializeObject<BookingAvailabilityInfo>(booking.ServiceDetails)
                : default;

            return new AccommodationBookingInfo(booking.Id,
                bookingDetails,
                serviceDetails,
                booking.CompanyId,
                booking.PaymentStatus);


            AccommodationBookingDetails GetDetails()
            {
                var details = JsonConvert.DeserializeObject<BookingDetails>(booking.BookingDetails);
                var roomDetails = details.RoomDetails
                    .Select(r => new BookingRoomDetailsWithPrice(
                        new BookingRoomDetails(r.RoomDetails.Type, r.RoomDetails.Passengers, r.RoomDetails.IsExtraBedNeeded), 
                        r.Prices))
                    .ToList();
                
                return new AccommodationBookingDetails(details.ReferenceCode,
                    details.Status,
                    details.CheckInDate,
                    details.CheckOutDate,
                    details.LocationDescription.CityCode,
                    details.AccommodationId,
                    details.TariffCode,
                    details.Deadline,
                    roomDetails);
            }
        }


        // TODO: Replace method when will be added other services 
        private Task<bool> AreExistBookingsForItn(string itn, int customerId)
            => _context.Bookings.Where(b => b.CustomerId == customerId && b.ItineraryNumber == itn).AnyAsync();


        private readonly EdoContext _context;
        private readonly ICustomerContext _customerContext;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ITagProcessor _tagProcessor;
        private readonly ILogger<BookingManager> _logger;
    }
}