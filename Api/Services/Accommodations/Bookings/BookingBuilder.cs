using System;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General.Enums;
using Newtonsoft.Json;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    internal class BookingBuilder
    {
        public BookingBuilder()
        {
            _booking = new Data.Booking.Booking {ServiceType = ServiceTypes.HTL};
        }


        public BookingBuilder(Data.Booking.Booking booking)
        {
            _booking = booking;
        }


        public BookingBuilder AddRequestInfo(in AccommodationBookingRequest bookingRequest)
        {
            _booking.Nationality = bookingRequest.Nationality;
            _booking.Residency = bookingRequest.Residency;
            _booking.MainPassengerName = bookingRequest.MainPassengerName;
            _booking.BookingRequest = JsonConvert.SerializeObject(bookingRequest);
            return this;
        }


        public BookingBuilder AddBookingDetails(in BookingDetails bookingDetails)
        {
            _booking.AgentReference = bookingDetails.AgentReference;
            _booking.BookingDetails = JsonConvert.SerializeObject(bookingDetails, JsonSerializerSettings);
            return this;
        }


        public BookingBuilder AddServiceDetails(in BookingAvailabilityInfo availabilityInfo)
        {
            _booking.ServiceDetails = JsonConvert.SerializeObject(availabilityInfo, JsonSerializerSettings);
            return this;
        }


        public BookingBuilder AddTags(string itn, string referenceNumber)
        {
            _booking.ItineraryNumber = itn;
            _booking.ReferenceCode = referenceNumber;
            return this;
        }


        public BookingBuilder AddCustomerInfo(CustomerInfo customerInfo)
        {
            _booking.CustomerId = customerInfo.CustomerId;
            _booking.CompanyId = customerInfo.CompanyId;
            return this;
        }


        public BookingBuilder AddCreationDate(DateTime date)
        {
            _booking.Created = date;
            return this;
        }


        public BookingBuilder AddStatus(BookingStatusCodes status)
        {
            _booking.Status = status;
            return this;
        }


        public BookingBuilder AddPaymentMethod(PaymentMethods paymentMethods)
        {
            _booking.PaymentMethod = paymentMethods;
            return this;
        }


        public BookingBuilder AddPaymentStatus(BookingPaymentStatuses status)
        {
            _booking.PaymentStatus = status;
            return this;
        }


        public BookingBuilder AddProviderInfo(DataProviders dataProvider)
        {
            _booking.DataProvider = dataProvider;
            return this;
        }


        public Data.Booking.Booking Build() => _booking;

        private readonly Data.Booking.Booking _booking;

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
            {NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore};
    }
}