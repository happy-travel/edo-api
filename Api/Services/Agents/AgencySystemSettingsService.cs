using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Agents
{
    public class AgencySystemSettingsService : IAgencySystemSettingsService
    {
        public AgencySystemSettingsService(EdoContext context)
        {
            _context = context;
        }
        
        public async Task<Maybe<AgencyAvailabilitySearchSettings>> GetAvailabilitySearchSettings(int agencyId)
        {
            var settings = await _context.AgencySystemSettings
                .SingleOrDefaultAsync(s => s.AgencyId == agencyId);

            return settings?.AvailabilitySearchSettings;
        }
        
        private readonly EdoContext _context;
    }
}