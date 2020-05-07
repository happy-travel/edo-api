using System;
using System.Linq;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data.Booking;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.General.Enums;
using HappyTravel.Money.Models;
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


        public BookingBuilder AddLanguageCode(string languageCode)
        {
            _booking.LanguageCode = languageCode;
            return this;
        }


        public BookingBuilder AddBookingDetails(in BookingDetails bookingDetails)
        {
            _booking.SupplierReferenceCode = bookingDetails.BookingCode;
            _booking.DeadlineDate = bookingDetails.Deadline;
            _booking.CheckInDate = bookingDetails.CheckInDate;
            _booking.CheckOutDate = bookingDetails.CheckOutDate;
            _booking.SupplierReferenceCode = bookingDetails.AgentReference;

            _booking.Rooms = bookingDetails.RoomDetails
                .Select(r =>
                {
                    var totalSum = r.Prices.Sum(p => p.NetTotal);
                    var currency = r.Prices.First().Currency;
                    return new BookedRoom(r.RoomDetails.Type,
                        r.RoomDetails.Passengers, 
                        r.RoomDetails.IsExtraBedNeeded,
                        new MoneyAmount(totalSum, currency));
                })
                .ToList();
            
            return this;
        }


        public BookingBuilder AddServiceDetails(in BookingAvailabilityInfo availabilityInfo)
        {
            var price = availabilityInfo.RoomContractSet.Price;
            _booking.TotalPrice = price.NetTotal;
            _booking.Currency = price.Currency;
            _booking.LocationInfo = new LocationInfo(availabilityInfo.CountryName,
                availabilityInfo.LocalityName,
                availabilityInfo.ZoneName);
            
            _booking.AccommodationId = availabilityInfo.AccommodationId;
            _booking.AccommodationName = availabilityInfo.AccommodationName;
            
            _booking.ServiceDetails = JsonConvert.SerializeObject(availabilityInfo, JsonSerializerSettings);
            return this;
        }


        public BookingBuilder AddTags(string itn, string referenceNumber)
        {
            _booking.ItineraryNumber = itn;
            _booking.ReferenceCode = referenceNumber;
            return this;
        }


        public BookingBuilder AddAgentInfo(AgentInfo agentInfo)
        {
            _booking.AgentId = agentInfo.AgentId;
            _booking.CounterpartyId = agentInfo.CounterpartyId;
            return this;
        }


        public BookingBuilder AddCreationDate(DateTime date)
        {
            _booking.Created = date;
            return this;
        }


        public BookingBuilder AddBookingDate(DateTime date)
        {
            _booking.BookingDate = date;
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
        
        
        public BookingBuilder AddUpdateMode(BookingUpdateMode updateMode)
        {
            _booking.UpdateMode = updateMode;
            return this;
        }


        public Data.Booking.Booking Build() => _booking;

        private readonly Data.Booking.Booking _booking;

        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
            {NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore};
    }
}