using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.CodeGeneration;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

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

        public async Task<Result<AccommodationBookingDetails, ProblemDetails>> Book(AccommodationBookingRequest bookingRequest, 
            BookingAvailabilityInfo availability,
            string languageCode)
        {
            var (_, isFailure, customerInfo, error)  = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return ProblemDetailsBuilder.Fail<AccommodationBookingDetails>(error);

            var itn = !string.IsNullOrWhiteSpace(bookingRequest.ItineraryNumber) 
                ? bookingRequest.ItineraryNumber 
                : await _tagGenerator.GenerateItn();
            
            var referenceCode = await _tagGenerator.GenerateReferenceCode(ServiceTypes.HTL,
                availability.CountryCode,
                itn);
            
            return await ExecuteBookingRequest()
                .OnSuccess(SaveBookingResult);
            
            Task<Result<AccommodationBookingDetails, ProblemDetails>> ExecuteBookingRequest()
            {
                var innerRequest = new InnerAccommodationBookingRequest(bookingRequest,
                    availability, referenceCode);
                
                return _dataProviderClient.Post<InnerAccommodationBookingRequest, AccommodationBookingDetails>(
                    new Uri(_options.Netstorming + "hotels/booking", UriKind.Absolute),
                    innerRequest, languageCode);
            }

            Task SaveBookingResult(AccommodationBookingDetails confirmedBooking)
            {
                var booking = new AccommodationBookingBuilder()
                    .AddCustomerInfo(customerInfo)
                    .AddTags(itn, referenceCode)
                    .AddRequestInfo(bookingRequest)
                    .AddConfirmationDetails(confirmedBooking)
                    .AddServiceDetails(availability)
                    .AddCreationDate(_dateTimeProvider.UtcNow())
                    .Build();

                _context.Bookings.Add(booking);
                return _context.SaveChangesAsync();
            }
        }

        public async Task<List<AccommodationBookingInfo>> Get()
        {
            var (_, isFailure, customerData, _) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return new List<AccommodationBookingInfo>(0);

            return await _context.Bookings
                .Where(b => b.CustomerId == customerData.CustomerId)
                .Select(b => new AccommodationBookingInfo(b.Id, b.BookingDetails, b.ServiceDetails, b.CompanyId))
                .ToListAsync();
        }

        public async Task<Result<Booking, ProblemDetails>> Cancel(int bookingId)
        {
            var (_, isFailure, customerData, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return ProblemDetailsBuilder.Fail<Booking>(error);
            
            var booking = await _context.Bookings
                .SingleOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerData.CustomerId);

            if (booking is null)
                return ProblemDetailsBuilder.Fail<Booking>($"Could not find booking with id '{bookingId}'");
            
            if(booking.Status == BookingStatusCodes.Cancelled)
                return ProblemDetailsBuilder.Fail<Booking>("Booking was already cancelled");
            
            return await ExecuteBookingCancel()
                .OnSuccess(async voidObj => await ChangeBookingToCancelled(booking));
            
            Task<Result<VoidObject, ProblemDetails>> ExecuteBookingCancel()
            {
                return _dataProviderClient.Post(new Uri(_options.Netstorming + "hotels/booking/" + booking.ReferenceCode + "/cancel", 
                    UriKind.Absolute));
            }

            async Task<Booking> ChangeBookingToCancelled(Booking bookingToCancel)
            {
                bookingToCancel.Status = BookingStatusCodes.Cancelled;
                if (booking.PaymentStatus == BookingPaymentStatuses.MoneyFrozen)
                    booking.PaymentStatus = BookingPaymentStatuses.Cancelled;
                var currentDetails = JsonConvert.DeserializeObject<AccommodationBookingDetails>(bookingToCancel.BookingDetails);
                bookingToCancel.BookingDetails = JsonConvert.SerializeObject(new AccommodationBookingDetails(currentDetails,
                        BookingStatusCodes.Cancelled));

                _context.Update(bookingToCancel);
                await _context.SaveChangesAsync();
                return bookingToCancel;
            }
        }


        public async Task<Result> ChangePaymentStatusForBookingToFrozen(int bookingId)
        {
            
            var (_, isFailure, customerData, error) = await _customerContext.GetCustomerInfo();
            if (isFailure)
                return Result.Fail(error);
            
            var booking = await _context.Bookings
                .SingleOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerData.CustomerId);
            
            if (booking is null)
                return Result.Fail($"Could not find booking with id '{bookingId}'");
            
            if (booking.PaymentStatus == BookingPaymentStatuses.MoneyFrozen)
                return Result.Fail("Booking was already has status MoneyFrozen");

            await ChangeStatus(booking);

            return Result.Ok();

            Task ChangeStatus(Booking bookingToFreeze)
            {
                bookingToFreeze.PaymentStatus = BookingPaymentStatuses.MoneyFrozen;
                _context.Update(bookingToFreeze);
                return _context.SaveChangesAsync();
            }
        }


        private readonly EdoContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICustomerContext _customerContext;
        private readonly ITagGenerator _tagGenerator;
        private readonly IDataProviderClient _dataProviderClient;
        private readonly DataProviderOptions _options;
    }
}