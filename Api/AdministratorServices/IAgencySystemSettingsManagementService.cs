using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data.Agents;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public interface IAgencySystemSettingsManagementService
    {
        Task<Result<AgencyAccommodationBookingSettings>> GetAvailabilitySearchSettings(int agencyId);

        Task<Result> SetAvailabilitySearchSettings(int agencyId, AgencyAccommodationBookingSettings settings);
    }
}