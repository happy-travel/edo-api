using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data.Agents;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public interface IAgencySystemSettingsService
    {
        Task<Maybe<AgencyAvailabilitySearchSettings>> GetAvailabilitySearchSettings(int agencyId);
    }
}