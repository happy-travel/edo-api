using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Data.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public interface IDeadlineService
    {
        Task<Result<Deadline, ProblemDetails>> GetDeadlineDetails(Guid searchId, Guid resultId, Guid roomContractSetId, AgentContext agent, string languageCode);
    }
}