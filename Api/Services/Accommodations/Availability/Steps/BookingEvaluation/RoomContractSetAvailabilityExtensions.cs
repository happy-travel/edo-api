using System.Collections.Generic;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.General.Enums;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public static class RoomContractSetAvailabilityExtensions
    {
        public static RoomContractSetAvailability? ToRoomContractSetAvailability(this in EdoContracts.Accommodations.RoomContractSetAvailability? availability, Suppliers? supplier, bool isDirectContract, List<PaymentTypes> paymentMethods)
        {
            if (availability is null)
                return null;

            var availabilityValue = availability.Value;
            return new RoomContractSetAvailability(availabilityValue.AvailabilityId,
                availabilityValue.CheckInDate,
                availabilityValue.CheckOutDate,
                availabilityValue.NumberOfNights,
                availabilityValue.Accommodation,
                availabilityValue.RoomContractSet.ToRoomContractSet(supplier, isDirectContract, creditCardPaymentCommission),
                paymentMethods);
        }
    }
}