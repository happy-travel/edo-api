using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Markups;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.EdoContracts.Accommodations;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.BookingEvaluation
{
    public interface IBookingEvaluationStorage
    {
        Task Set(Guid searchId, Guid resultId, Guid roomContractSetId, DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> availability, DataProviders resultDataProvider);
        
        Task<Result<(DataProviders Source, DataWithMarkup<SingleAccommodationAvailabilityDetailsWithDeadline> Result)>> Get(Guid searchId, Guid resultId, Guid roomContractSetId, List<DataProviders> dataProviders);
    }
}