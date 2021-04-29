using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Accommodations;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using RoomContractSetAvailability = HappyTravel.EdoContracts.Accommodations.RoomContractSetAvailability;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public interface IBookingEvaluationStorage
    {
        Task Set(Guid searchId, Guid resultId, Guid roomContractSetId, DataWithMarkup<RoomContractSetAvailability> availability, Suppliers resultSupplier,
            List<PaymentTypes> availablePaymentTypes, string htId);
        
        Task<Result<BookingAvailabilityInfo>> Get(Guid searchId, Guid resultId, Guid roomContractSetId);
    }
}