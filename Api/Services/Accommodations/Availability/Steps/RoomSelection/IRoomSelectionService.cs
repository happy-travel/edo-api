using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.EdoContracts.Accommodations;
using HappyTravel.EdoContracts.Accommodations.Internals;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability.Steps.RoomSelection
{
    public interface IRoomSelectionService
    {
        Task<AvailabilitySearchTaskState> GetState(Guid searchId, Guid resultId);

        Task<Result<AccommodationDetails, ProblemDetails>> GetAccommodation(Guid searchId, Guid resultId, string languageCode);

        Task<Result<List<RoomContractSet>>> Get(Guid searchId, Guid resultId, AgentContext agent, string languageCode);
    }
}