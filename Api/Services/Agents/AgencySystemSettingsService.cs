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


        public async Task<Result<DisplayedPaymentOptionsSettings>> GetDisplayedPaymentOptions(AgentContext agentContext)
        {
            return await Result.Success()
                .Ensure(() => DoesAgencyExist(agentContext.AgencyId), "Agency with such id does not exist")
                .Map(GetOptions);


            async Task<DisplayedPaymentOptionsSettings> GetOptions()
            {
                var systemSettings = await _context.AgencySystemSettings.SingleOrDefaultAsync(s => s.AgencyId == agentContext.AgencyId);
                return systemSettings?.DisplayedPaymentOptions ?? DefaultDisplayedPaymentOptionsSettings;
            }
        }


        private Task<bool> DoesAgencyExist(int agencyId) => _context.Agencies.AnyAsync(a => a.Id == agencyId && a.IsActive);

        private const DisplayedPaymentOptionsSettings DefaultDisplayedPaymentOptionsSettings = DisplayedPaymentOptionsSettings.CreditCardAndBankTransfer;

        private readonly EdoContext _context;
    }
}