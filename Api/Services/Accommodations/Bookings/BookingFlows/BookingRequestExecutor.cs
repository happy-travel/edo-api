using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Bookings;
using HappyTravel.Edo.Api.Services.Connectors;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Enums;
using HappyTravel.EdoContracts.Accommodations.Internals;

namespace HappyTravel.Edo.Api.Services.Accommodations.Bookings
{
    public class BookingRequestExecutor
    {
        private readonly ISupplierConnectorManager _supplierConnectorManager;


        public BookingRequestExecutor(ISupplierConnectorManager supplierConnectorManager)
        {
            _supplierConnectorManager = supplierConnectorManager;
        }
        
        
        public async Task<Booking> SendSupplierRequest(AccommodationBookingRequest bookingRequest, string availabilityId, Data.Bookings.Booking booking, string referenceCode, string languageCode)
        {
            var features = new List<Feature>(); //bookingRequest.Features

            var roomDetails = bookingRequest.RoomDetails
                .Select(d => new SlimRoomOccupation(d.Type, d.Passengers, string.Empty, d.IsExtraBedNeeded))
                .ToList();

            var innerRequest = new BookingRequest(availabilityId,
                bookingRequest.RoomContractSetId,
                booking.ReferenceCode,
                roomDetails,
                features,
                bookingRequest.RejectIfUnavailable);

            try
            {
                var bookingResult = await _supplierConnectorManager
                    .Get(booking.Supplier)
                    .Book(innerRequest, languageCode);

                if (bookingResult.IsSuccess)
                {
                    return bookingResult.Value;
                }

                return GetStubDetails(booking);
            }
            catch
            {
                var errorMessage = $"Failed to update booking data (refcode '{referenceCode}') after the request to the connector";

                var (_, isCancellationFailed, cancellationError) = await _supplierConnectorManager.Get(booking.Supplier).CancelBooking(booking.ReferenceCode);
                if (isCancellationFailed)
                    errorMessage += Environment.NewLine + $"Booking cancellation has failed: {cancellationError}";


                return GetStubDetails(booking);
            }


            // TODO: Remove room information and contract description from booking NIJO-915
            static EdoContracts.Accommodations.Booking GetStubDetails(Data.Bookings.Booking booking)
                => new EdoContracts.Accommodations.Booking(booking.ReferenceCode,
                    // Will be set in the refresh step
                    BookingStatusCodes.WaitingForResponse,
                    booking.AccommodationId,
                    booking.SupplierReferenceCode,
                    booking.CheckInDate,
                    booking.CheckOutDate,
                    new List<SlimRoomOccupation>(0),
                    BookingUpdateModes.Asynchronous);
        }
    }
}