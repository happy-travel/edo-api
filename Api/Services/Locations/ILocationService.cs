using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Availabilities;
using HappyTravel.Edo.Api.Models.Locations;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Services.Locations
{
    public interface ILocationService
    {
        ValueTask<Result<Location, ProblemDetails>> Get(SearchLocation searchLocation, string languageCode);

        Task<List<Country>> GetCountries(string query, string languageCode);

        ValueTask<Result<List<Prediction>, ProblemDetails>> GetPredictions(string query, string session, AgentContext agent, string languageCode);

        Task<List<Region>> GetRegions(string languageCode);
    }
}