using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AgencySystemSettingsManagementService : IAgencySystemSettingsManagementService
    {
        public AgencySystemSettingsManagementService(EdoContext context)
        {
            _context = context;
        }


        public async Task<Result<AgencyAccommodationBookingSettings>> GetAvailabilitySearchSettings(int agencyId)
        {
            return await CheckAgencyExists(agencyId)
                .Bind(GetSettings);


            async Task<Result<AgencyAccommodationBookingSettings>> GetSettings()
            {
                var existingSettings = await _context.AgencySystemSettings.SingleOrDefaultAsync(s => s.AgencyId == agencyId);
                
                return existingSettings == default
                    ? Result.Failure<AgencyAccommodationBookingSettings>($"Could not find availability search settings for agency with id {agencyId}")
                    : existingSettings.AccommodationBookingSettings;
            }
        }


        public async Task<Result> SetAvailabilitySearchSettings(int agencyId, AgencyAccommodationBookingSettings settings)
        {
            return await CheckAgencyExists(agencyId)
                .Bind(SetSettings);


            async Task<Result> SetSettings()
            {
                if (settings.CustomDeadlineShift.HasValue && settings.CustomDeadlineShift >= 0) 
                    return Result.Failure("Deadline shift must be less than zero");
                
                var existingSettings = await _context.AgencySystemSettings.SingleOrDefaultAsync(s => s.AgencyId == agencyId);
                if (existingSettings == default)
                {
                    var newSettings = new AgencySystemSettings
                    {
                        AgencyId = agencyId,
                        AccommodationBookingSettings = settings
                    };
                    _context.AgencySystemSettings.Add(newSettings);
                }
                else
                {
                    existingSettings.AccommodationBookingSettings = settings;
                    _context.Update(existingSettings);
                }

                await _context.SaveChangesAsync();
                return Result.Success();
            }
        }


        private async Task<Result> CheckAgencyExists(int agencyId)
            => await DoesAgencyExist(agencyId)
                ? Result.Success()
                : Result.Failure("Agency with such id does not exist");


        private Task<bool> DoesAgencyExist(int agencyId) 
            => _context.Agencies.AnyAsync(a => a.Id == agencyId && a.IsActive);


        private readonly EdoContext _context;
    }
}