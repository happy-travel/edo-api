using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.AdministratorServices
{
    public class AgentSystemSettingsManagementService : IAgentSystemSettingsManagementService
    {
        public AgentSystemSettingsManagementService(EdoContext context)
        {
            _context = context;
        }


        public async Task<Result> SetAvailabilitySearchSettings(int agentId, int agencyId, AgentAvailabilitySearchSettings settings)
        {
            if (settings.EnabledProviders is null || !settings.EnabledProviders.Any())
                return Result.Failure("Invalid data providers");

            var doesRelationExist = await _context.AgentAgencyRelations
                .AnyAsync(r => r.AgentId == agentId && r.AgencyId == agencyId);

            if (!doesRelationExist)
                return Result.Failure("Could not find specified agent in given agency");

            var existingSettings = await _context.AgentSystemSettings.SingleOrDefaultAsync(s => s.AgentId == agentId && s.AgencyId == agencyId);
            if (existingSettings is null)
            {
                var newSettings = new AgentSystemSettings
                {
                    AgencyId = agencyId,
                    AgentId = agentId,
                    AvailabilitySearchSettings = settings
                };
                _context.Add(newSettings);
            }
            else
            {
                existingSettings.AvailabilitySearchSettings = settings;
                _context.Update(existingSettings);
            }

            await _context.SaveChangesAsync();
            return Result.Ok();
        }


        public async Task<Result<AgentAvailabilitySearchSettings>> GetAvailabilitySearchSettings(int agentId, int agencyId)
        {
            var doesRelationExist = await _context.AgentAgencyRelations
                .AnyAsync(r => r.AgentId == agentId || r.AgencyId == agencyId);

            if (!doesRelationExist)
                return Result.Failure<AgentAvailabilitySearchSettings>("Could not find specified agent in given agency");

            var existingSettings = await _context.AgentSystemSettings.SingleOrDefaultAsync(s => s.AgentId == agentId && s.AgencyId == agencyId);
            return existingSettings == default
                ? Result.Failure<AgentAvailabilitySearchSettings>("Could not find settings for specified agent in given agency")
                : existingSettings.AvailabilitySearchSettings;
        } 
        
        
        private readonly EdoContext _context;
    }
}