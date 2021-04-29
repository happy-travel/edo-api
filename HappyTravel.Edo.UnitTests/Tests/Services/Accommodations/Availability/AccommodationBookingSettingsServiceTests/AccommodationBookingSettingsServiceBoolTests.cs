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
using HappyTravel.Edo.Data.Agents;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Accommodations.Availability.AccommodationBookingSettingsServiceTests
{
    public class AccommodationBookingSettingsServiceBoolTests
    {
        // Settings combined from two sources
        [Fact]
        public async Task Combined_bool_settings_must_be_false_if_no_source_found()
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

            Assert.False(settings.IsMarkupDisabled);
            Assert.False(settings.IsSupplierVisible);
            Assert.False(settings.IsDirectContractFlagVisible);
        }


        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task Agent_combined_bool_settings_must_apply_when_only_agent_settings_found(
            bool expectedMarkupDisabled,
            bool expectedSupplierVisible,
            bool expectedDirectContractFlagVisible)
        {
            var agentSettings = Maybe<AgentAccommodationBookingSettings>
                .From(new AgentAccommodationBookingSettings
                {
                    IsMarkupDisabled = expectedMarkupDisabled,
                    IsSupplierVisible = expectedSupplierVisible,
                    IsDirectContractFlagVisible = expectedDirectContractFlagVisible
                });
            var agencySettings = default(Maybe<AgencyAccommodationBookingSettings>);
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(expectedMarkupDisabled, settings.IsMarkupDisabled);
            Assert.Equal(expectedSupplierVisible, settings.IsSupplierVisible);
            Assert.Equal(expectedDirectContractFlagVisible, settings.IsDirectContractFlagVisible);
        }


        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task Agency_combined_bool_settings_must_apply_when_only_agency_settings_found(
            bool expectedMarkupDisabled,
            bool expectedSupplierVisible,
            bool expectedDirectContractFlagVisible)
        {
            var agentSettings = default(Maybe<AgentAccommodationBookingSettings>);
            var agencySettings = Maybe<AgencyAccommodationBookingSettings>
                .From(new AgencyAccommodationBookingSettings
                {
                    IsMarkupDisabled = expectedMarkupDisabled,
                    IsSupplierVisible = expectedSupplierVisible,
                    IsDirectContractFlagVisible = expectedDirectContractFlagVisible
                });
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(expectedMarkupDisabled, settings.IsMarkupDisabled);
            Assert.Equal(expectedSupplierVisible, settings.IsSupplierVisible);
            Assert.Equal(expectedDirectContractFlagVisible, settings.IsDirectContractFlagVisible);
        }


        [Theory]
        [InlineData(false, false, false, true, true, true, true, false, true)]
        [InlineData(true, false, true, false, false, false, true, true, false)]
        [InlineData(true, true, false, true, false, true, true, true, true)]
        [InlineData(true, true, true, true, true, false, false, false, false)]
        public async Task Combined_bool_settings_must_be_true_if_any_source_gives_true(
            bool expectedMarkupDisabled, bool agentMarkupDisabled, bool agencyMarkupDisabled,
            bool expectedSupplierVisible, bool agentSupplierVisible, bool agencySupplierVisible,
            bool expectedDirectContractFlagVisible, bool agentDirectContractFlagVisible, bool agencyDirectContractFlagVisible)
        {
            var agentSettings = Maybe<AgentAccommodationBookingSettings>
                .From(new AgentAccommodationBookingSettings
                {
                    IsMarkupDisabled = agentMarkupDisabled,
                    IsSupplierVisible = agentSupplierVisible,
                    IsDirectContractFlagVisible = agentDirectContractFlagVisible
                });
            var agencySettings = Maybe<AgencyAccommodationBookingSettings>
                .From(new AgencyAccommodationBookingSettings
                {
                    IsMarkupDisabled = agencyMarkupDisabled,
                    IsSupplierVisible = agencySupplierVisible,
                    IsDirectContractFlagVisible = agencyDirectContractFlagVisible
                });
            var counterpartySettings = default(CounterpartyAccommodationBookingSettings);

            var (agentSettingsService, agencySettingsService, counterpartySettingsService) = GetSettingsServices(agentSettings, agencySettings, counterpartySettings);
            var supplierOptions = Options.Create(new SupplierOptions { EnabledSuppliers = new List<Suppliers> { Suppliers.Unknown } });
            var flow = GetDoubleFlow();

            var service = new AccommodationBookingSettingsService(flow, agentSettingsService, agencySettingsService, counterpartySettingsService,
                supplierOptions);

            var settings = await service.Get(_agentContext);

            Assert.Equal(expectedMarkupDisabled, settings.IsMarkupDisabled);
            Assert.Equal(expectedSupplierVisible, settings.IsSupplierVisible);
            Assert.Equal(expectedDirectContractFlagVisible, settings.IsDirectContractFlagVisible);
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
    }
}
