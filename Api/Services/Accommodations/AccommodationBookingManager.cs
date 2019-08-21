using System;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Booking;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Accommodations
{
    public class AccommodationBookingManager : IAccommodationBookingManager
    {
        public AccommodationBookingManager(IOptions<DataProviderOptions> options, IDataProviderClient dataProviderClient, 
            EdoContext context,
            IAvailabilityResultsCache availabilityResultsCache,
            IDateTimeProvider dateTimeProvider,
            ICustomerContext customerContext)
        {
            _dataProviderClient = dataProviderClient;
            _options = options.Value;
            _context = context;
            _availabilityResultsCache = availabilityResultsCache;
            _dateTimeProvider = dateTimeProvider;
            _customerContext = customerContext;
        }

        public async Task<Result<AccommodationBookingDetails, ProblemDetails>> Book(AccommodationBookingRequest request,
            string languageCode)
        {
            var (_, isFailure, customer, error) = await  _customerContext.GetCurrent();
            if (isFailure)
                return ProblemDetailsBuilder.BuildFailResult<AccommodationBookingDetails>(error);
            
            var itn = await _context.GetNextItineraryNumber();
            var referenceCode = ReferenceCodeGenerator.Generate(ServiceTypes.HTL, request.Residency, itn);

            var inner = new InnerAccommodationBookingRequest(request, referenceCode);

            return await ExecuteBookingRequest(inner)
                .OnSuccess(booking => SaveResults(booking, request, customer.Id));

            Task<Result<AccommodationBookingDetails, ProblemDetails>> ExecuteBookingRequest(in InnerAccommodationBookingRequest innerRequest)
            {
                return _dataProviderClient.Post<InnerAccommodationBookingRequest, AccommodationBookingDetails>(
                    new Uri(_options.Netstorming + "hotels/booking", UriKind.Absolute),
                    innerRequest, languageCode);
            }
        }

        private async Task SaveResults(AccommodationBookingDetails bookedDetails,
            AccommodationBookingRequest request, int customerId)
        {
            var availabilityResponse = await _availabilityResultsCache.Get(request.AvailabilityId);
            var (chosenResult, chosenAgreement) = (from availabilityResult in availabilityResponse.Results
                    from agreement in availabilityResult.Agreements
                    where agreement.Id == request.AgreementId
                    select (availabilityResult, agreement))
                .Single();
                
            var accommodationDetails = chosenResult.AccommodationDetails;
            var location = accommodationDetails.Location;

            var booking = CreateBooking();
            _context.AccommodationBookings.Add(booking);

            foreach (var roomDetails in bookedDetails.RoomDetails)
            {
                var bookingRoom = CreateBookingRoom(booking, roomDetails);
                _context.AccommodationBookingRoomDetails.Add(bookingRoom);

                foreach (var pax in roomDetails.RoomDetails.Passengers)
                    _context.AccommodationBookingPassengers.Add(CreatePassenger(pax, bookingRoom));
            }

            await _context.SaveChangesAsync();

            AccommodationBooking CreateBooking()
            {
                return new AccommodationBooking
                {
                    BookingDate = _dateTimeProvider.UtcNow(),
                    Deadline = bookedDetails.Deadline,
                    Status = bookedDetails.Status,
                    AccommodationId = bookedDetails.AccommodationId,
                    ReferenceCode = bookedDetails.ReferenceCode,
                
                    Service = accommodationDetails.Name,
                    TariffCode = bookedDetails.TariffCode,
                    ContractTypeId = bookedDetails.ContractTypeId,
                
                    // Location
                    AgentReference = request.AgentReference,
                    Nationality = request.Nationality,
                    Residency = request.Residency,

                    CheckInDate = bookedDetails.CheckInDate,
                    CheckOutDate = bookedDetails.CheckOutDate,
                    RateBasis = chosenAgreement.BoardBasis,
                
                    PriceCurrency = Enum.Parse<Currencies>(chosenAgreement.CurrencyCode), 
                    CountryCode = location.CountryCode,
                    CityCode = location.CityCode,
                    Features = chosenAgreement.Remarks,
                
                    CustomerId = customerId
                };
            }

            AccomodationBookingRoomDetails CreateBookingRoom(AccommodationBooking accommodationBooking, BookingRoomDetailsWithPrice roomDetails)
            {
                return new AccomodationBookingRoomDetails()
                {
                    AccommodationBookingId = accommodationBooking.Id,
                    Price = roomDetails.Price.Price,
                    CotPrice = roomDetails.Price.CotPrice,
                    ExtraBedPrice = roomDetails.Price.ExtraBedPrice,
                    Type = roomDetails.RoomDetails.Type,
                    IsCotNeededNeeded = roomDetails.RoomDetails.IsCotNeededNeeded,
                    IsExtraBedNeeded = roomDetails.RoomDetails.IsExtraBedNeeded
                };
            }

            AccomodationBookingPassenger CreatePassenger(Pax pax, AccomodationBookingRoomDetails bookingRoom)
            {
                return new AccomodationBookingPassenger
                {
                    Age = pax.Age,
                    Initials = pax.Initials,
                    Title = pax.Title,
                    FirstName = pax.FirstName,
                    IsLeader = pax.IsLeader,
                    LastName = pax.LastName,
                    BookingRoomDetailsId = bookingRoom.Id
                };
            }
        }

        private readonly EdoContext _context;
        private readonly IAvailabilityResultsCache _availabilityResultsCache;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ICustomerContext _customerContext;
        private readonly IDataProviderClient _dataProviderClient;
        private readonly DataProviderOptions _options;
    }
}