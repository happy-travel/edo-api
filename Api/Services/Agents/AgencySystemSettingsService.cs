using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Common.Enums.AgencySettings;
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


        public async Task<Maybe<AgencyAccommodationBookingSettings>> GetAccommodationBookingSettings(int agencyId)
        {
            var settings = await _context.AgencySystemSettings
                .SingleOrDefaultAsync(s => s.AgencyId == agencyId);

            return settings?.AccommodationBookingSettings;
        }


        private readonly EdoContext _context;
    }
}