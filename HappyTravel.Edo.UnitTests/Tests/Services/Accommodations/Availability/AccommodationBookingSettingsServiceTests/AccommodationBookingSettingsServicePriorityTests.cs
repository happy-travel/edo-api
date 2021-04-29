using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Common.Enums.AgencySettings;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.EdoContracts.General.Enums;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.AccommodationBookingSettingsServiceTests
{
    public class AccommodationBookingSettingsServicePriorityTests
    {
        // Default settings
        [Fact]
        public async Task Defaults_should_apply_when_no_agent_and_agency_settings_found()
        {
            var agentSettings = default(Maybe<AgentAccommodationBookingSettings>);
            var agencySettings = default(Maybe<AgencyAccommodationBookingSettings>);
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(DefaultAprMode, settings.AprMode);
            Assert.Equal(DefaultPassedDeadlineOffersMode, settings.PassedDeadlineOffersMode);
            Assert.Equal(default, settings.AdditionalSearchFilters);
            
            for (int i = 0; i < supplierOptions.Value.EnabledSuppliers.Count; i++)
                Assert.Equal(supplierOptions.Value.EnabledSuppliers[i], settings.EnabledConnectors[i]);
        }


        // Settings from one source
        [Fact]
        public async Task Counterparty_setting_must_apply_exactly()
        {
            var agentSettings = default(Maybe<AgentAccommodationBookingSettings>);
            var agencySettings = default(Maybe<AgencyAccommodationBookingSettings>);
            var counterpartySettings = new CounterpartyAccommodationBookingSettings
            {
                CancellationPolicyProcessSettings = new CancellationPolicyProcessSettings
                {
                    PolicyStartDateShift = new TimeSpan(0, 0, 0, 18)
                }
            };
            var expectedPolicyStartDateShift = new TimeSpan(0, 0, 0, 18);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(expectedPolicyStartDateShift, settings.CancellationPolicyProcessSettings.PolicyStartDateShift);
        }


        [Fact]
        public async Task AdditionalSearchFilters_setting_must_apply_exactly_when_agent_settings_found()
        {
            var agentSettings = Maybe<AgentAccommodationBookingSettings>
                .From(new AgentAccommodationBookingSettings{ AdditionalSearchFilters = SearchFilters.BestPrice | SearchFilters.BestRoomPlans });
            var agencySettings = default(Maybe<AgencyAccommodationBookingSettings>);
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(SearchFilters.BestPrice | SearchFilters.BestRoomPlans, settings.AdditionalSearchFilters);
        }


        // Settings taken from one of two sources (agent setting is priority)
        [Fact]
        public async Task Agent_settings_must_apply_when_only_agent_settings_found()
        {
            var expectedSupplierOptions = new List<Suppliers> { Suppliers.Columbus, Suppliers.Etg };
            var agentSettings = Maybe<AgentAccommodationBookingSettings>
                .From(new AgentAccommodationBookingSettings
                {
                    EnabledSuppliers = new List<Suppliers> { Suppliers.Columbus, Suppliers.Etg },
                    AprMode = AprMode.CardPurchasesOnly,
                    PassedDeadlineOffersMode = PassedDeadlineOffersMode.CardAndAccountPurchases
                });
            var agencySettings = default(Maybe<AgencyAccommodationBookingSettings>);
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(AprMode.CardPurchasesOnly, settings.AprMode);
            Assert.Equal(PassedDeadlineOffersMode.CardAndAccountPurchases, settings.PassedDeadlineOffersMode);

            for (int i = 0; i < expectedSupplierOptions.Count; i++)
                Assert.Equal(expectedSupplierOptions[i], settings.EnabledConnectors[i]);
        }


        [Fact]
        public async Task Agent_settings_must_apply_when_agent_and_agency_settings_found()
        {
            var expectedSupplierOptions = new List<Suppliers> { Suppliers.Columbus, Suppliers.Etg };
            var agentSettings = Maybe<AgentAccommodationBookingSettings>
                .From(new AgentAccommodationBookingSettings
                {
                    EnabledSuppliers = new List<Suppliers> { Suppliers.Columbus, Suppliers.Etg },
                    AprMode = AprMode.CardPurchasesOnly,
                    PassedDeadlineOffersMode = PassedDeadlineOffersMode.CardAndAccountPurchases
                });
            var agencySettings = Maybe<AgencyAccommodationBookingSettings>
                .From(new AgencyAccommodationBookingSettings
                {
                    EnabledSuppliers = new List<Suppliers> { Suppliers.Illusions, Suppliers.Rakuten },
                    AprMode = AprMode.Hide,
                    PassedDeadlineOffersMode = PassedDeadlineOffersMode.Hide
                });
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(AprMode.CardPurchasesOnly, settings.AprMode);
            Assert.Equal(PassedDeadlineOffersMode.CardAndAccountPurchases, settings.PassedDeadlineOffersMode);

            for (int i = 0; i < expectedSupplierOptions.Count; i++)
                Assert.Equal(expectedSupplierOptions[i], settings.EnabledConnectors[i]);
        }


        [Fact]
        public async Task Agency_settings_must_apply_when_only_agency_settings_found()
        {
            var expectedSupplierOptions = new List<Suppliers> { Suppliers.Illusions, Suppliers.Rakuten };
            var agentSettings = default(Maybe<AgentAccommodationBookingSettings>);
            var agencySettings = Maybe<AgencyAccommodationBookingSettings>
                .From(new AgencyAccommodationBookingSettings
                {
                    EnabledSuppliers = new List<Suppliers> { Suppliers.Illusions, Suppliers.Rakuten },
                    AprMode = AprMode.Hide,
                    PassedDeadlineOffersMode = PassedDeadlineOffersMode.Hide
                });
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(AprMode.Hide, settings.AprMode);
            Assert.Equal(PassedDeadlineOffersMode.Hide, settings.PassedDeadlineOffersMode);

            for (int i = 0; i < expectedSupplierOptions.Count; i++)
                Assert.Equal(expectedSupplierOptions[i], settings.EnabledConnectors[i]);
        }


        private IDoubleFlow GetDoubleFlow()
        {
            var mock = new Mock<IDoubleFlow>();
            mock.Setup(m => m.GetOrSetAsync(
                    It.IsAny<string>(),
                    It.IsAny<Func<Task<AccommodationBookingSettings>>>(),
                    It.IsAny<TimeSpan>(),
                    default))
                .Returns(
                    (string _, Func<Task<AccommodationBookingSettings>> b, TimeSpan _, CancellationToken _) => b());

            mock.Setup(m => m.Options)
                .Returns(new FlowOptions {CacheKeyDelimiter = ", "});

            return mock.Object;
        }

        private (IAgentSystemSettingsService, IAgencySystemSettingsService, ICounterpartySystemSettingsService) GetSettingsServices(
            Maybe<AgentAccommodationBookingSettings> agentSettings,
            Maybe<AgencyAccommodationBookingSettings> agencySettings,
            CounterpartyAccommodationBookingSettings counterpartySettings)
        {
            var agentMock = new Mock<IAgentSystemSettingsService>();
            agentMock.Setup(m => m.GetAccommodationBookingSettings(It.IsAny<AgentContext>()))
                .Returns(() => Task.FromResult(agentSettings));
            
            var agencyMock = new Mock<IAgencySystemSettingsService>();
            agencyMock.Setup(m => m.GetAccommodationBookingSettings(It.IsAny<int>()))
                .Returns(() => Task.FromResult(agencySettings));
            
            var counterpartyMock = new Mock<ICounterpartySystemSettingsService>();
            counterpartyMock.Setup(m => m.GetAccommodationBookingSettings(It.IsAny<int>()))
                .Returns(() => Task.FromResult(counterpartySettings));

            return (agentMock.Object, agencyMock.Object, counterpartyMock.Object);
        }


        private readonly AgentContext _agentContext = new AgentContext(1, "fn", "ln", "email", "title", "pos", 1, "cname", 1, default, default);

        private const PassedDeadlineOffersMode DefaultPassedDeadlineOffersMode = PassedDeadlineOffersMode.DisplayOnly;
        private const AprMode DefaultAprMode = AprMode.DisplayOnly;
    }
}
