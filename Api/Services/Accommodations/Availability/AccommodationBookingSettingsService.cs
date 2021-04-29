using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Options;

namespace HappyTravel.Edo.Api.Services.Accommodations.Availability
{
    public class AccommodationBookingSettingsService : IAccommodationBookingSettingsService
    {
        public AccommodationBookingSettingsService(IDoubleFlow doubleFlow,
            IAgentSystemSettingsService agentSystemSettingsService,
            IAgencySystemSettingsService agencySystemSettingsService,
            ICounterpartySystemSettingsService counterpartySystemSettingsService,
            IOptions<SupplierOptions> supplierOptions)
        {
            _doubleFlow = doubleFlow;
            _agentSystemSettingsService = agentSystemSettingsService;
            _supplierOptions = supplierOptions.Value;
            _agencySystemSettingsService = agencySystemSettingsService;
            _counterpartySystemSettingsService = counterpartySystemSettingsService;
        }


        public Task<AccommodationBookingSettings> Get(AgentContext agent)
        {
            var key = _doubleFlow.BuildKey(nameof(AccommodationBookingSettingsService),
                nameof(Get),
                agent.AgentId.ToString(),
                agent.AgencyId.ToString());

            return _doubleFlow.GetOrSetAsync(key, async () =>
            {
                var agentSettings = await _agentSystemSettingsService.GetAccommodationBookingSettings(agent);
                var agencySettings = await _agencySystemSettingsService.GetAccommodationBookingSettings(agent.AgencyId);
                var counterpartySettings = await _counterpartySystemSettingsService.GetAccommodationBookingSettings(agent.CounterpartyId);

                return MergeSettings(agentSettings, agencySettings, counterpartySettings);
            }, SettingsCacheLifetime);
        }


        private AccommodationBookingSettings MergeSettings(Maybe<AgentAccommodationBookingSettings> agentSettings,
            Maybe<AgencyAccommodationBookingSettings> agencySettings, CounterpartyAccommodationBookingSettings counterpartySettings)
        {
            var agentSettingsValue = agentSettings.HasValue 
                ? agentSettings.Value
                : null;
            var agencySettingsValue = agencySettings.HasValue 
                ? agencySettings.Value
                : null;

            List<Suppliers> enabledConnectors = agentSettingsValue?.EnabledSuppliers ?? agencySettingsValue?.EnabledSuppliers ?? _supplierOptions.EnabledSuppliers;
            AprMode? aprMode = agentSettingsValue?.AprMode ?? agencySettingsValue?.AprMode ?? DefaultAprMode;
            PassedDeadlineOffersMode? passedDeadlineOffersMode = agentSettingsValue?.PassedDeadlineOffersMode ?? agencySettingsValue?.PassedDeadlineOffersMode ??
                DefaultPassedDeadlineOffersMode;

            bool isMarkupDisabled = agentSettingsValue?.IsMarkupDisabled == true || agencySettingsValue?.IsMarkupDisabled == true;
            bool isSupplierVisible = agentSettingsValue?.IsSupplierVisible == true || agencySettingsValue?.IsSupplierVisible == true;
            bool isDirectContractFlagVisible = agentSettingsValue?.IsDirectContractFlagVisible == true || agencySettingsValue?.IsDirectContractFlagVisible == true;

            SearchFilters additionalSearchFilters = agentSettingsValue?.AdditionalSearchFilters ?? default;

            var cancellationPolicyProcessSettings = counterpartySettings.CancellationPolicyProcessSettings;

            if (agencySettings.HasValue && agencySettings.Value.CustomDeadlineShift.HasValue)
            {
                cancellationPolicyProcessSettings = new CancellationPolicyProcessSettings
                {
                    PolicyStartDateShift = TimeSpan.FromDays(agencySettings.Value.CustomDeadlineShift.Value)
                };
            }

            return new AccommodationBookingSettings(enabledConnectors,
                aprMode.Value,
                passedDeadlineOffersMode.Value,
                isMarkupDisabled: isMarkupDisabled, 
                isSupplierVisible: isSupplierVisible,
                cancellationPolicyProcessSettings,
                isDirectContractFlagVisible: isDirectContractFlagVisible,
                additionalSearchFilters);
        }

        
        private const PassedDeadlineOffersMode DefaultPassedDeadlineOffersMode = PassedDeadlineOffersMode.DisplayOnly;

        private const AprMode DefaultAprMode = AprMode.DisplayOnly;
        
        private readonly IDoubleFlow _doubleFlow;
        private readonly IAgentSystemSettingsService _agentSystemSettingsService;
        private readonly IAgencySystemSettingsService _agencySystemSettingsService;
        private readonly SupplierOptions _supplierOptions;
        
        private static readonly TimeSpan SettingsCacheLifetime = TimeSpan.FromMinutes(5);
        private readonly ICounterpartySystemSettingsService _counterpartySystemSettingsService;
    }
}