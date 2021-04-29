using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums.Markup;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Edo.UnitTests.Mocks;
using HappyTravel.Edo.UnitTests.Utility;
using HappyTravel.Money.Enums;
using HappyTravel.Money.Models;
using Moq;
using NetTopologySuite.Utilities;
using Xunit;
using Assert = Xunit.Assert;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Markups.MarkupServiceTests
{
    public class MarkupsApplyingOrder : IDisposable
    {
        public MarkupsApplyingOrder()
        {
            var allPolicies = _agentPolicies
                .Union(_counterpartyPolicies)
                .Union(_globalPolicies)
                .Union(_agencyPolicies);
            
            var edoContextMock = MockEdoContextFactory.Create();
            edoContextMock.Setup(c => c.MarkupPolicies)
                .Returns(DbSetMockProvider.GetDbSetMock(allPolicies));
            
            edoContextMock.Setup(c => c.Agencies)
                .Returns(DbSetMockProvider.GetDbSetMock(_agencies));
            
            var currencyRateServiceMock = new Mock<ICurrencyRateService>();
            currencyRateServiceMock
                .Setup(c => c.Get(It.IsAny<Currencies>(), It.IsAny<Currencies>()))
                .Returns(new ValueTask<Result<decimal>>(Result.Success((decimal)1)));
    
            var agentSettingsMock = new Mock<IAgentSettingsManager>();
            
            agentSettingsMock
                .Setup(s => s.GetUserSettings(It.IsAny<AgentContext>()))
                .Returns(Task.FromResult(new AgentUserSettings(true, It.IsAny<Currencies>(), It.IsAny<Currencies>(), It.IsAny<int>())));
            
            var accommodationBookingSettingsServiceMock = new Mock<IAccommodationBookingSettingsService>();
            accommodationBookingSettingsServiceMock
                .Setup(s => s.Get(It.IsAny<AgentContext>()))
                .ReturnsAsync(new AccommodationBookingSettings(default, default, default, isMarkupDisabled: false, isSupplierVisible: default, default, isDirectContractFlagVisible: default, default));
                
                
            _markupPolicyService = new MarkupPolicyService(edoContextMock.Object,
                new FakeDoubleFlow(), 
                agentSettingsMock.Object,
                accommodationBookingSettingsServiceMock.Object);

            var discountServiceMock = new  Mock<IDiscountFunctionService>();
            discountServiceMock.Setup(service => service.Get(It.IsAny<MarkupPolicy>(), It.IsAny<AgentContext>()))
                .ReturnsAsync((price => new ValueTask<MoneyAmount>(price)));
            
            _markupService = new MarkupService(_markupPolicyService, discountServiceMock.Object, new MarkupPolicyTemplateService(), currencyRateServiceMock.Object, new FakeMemoryFlow());
        }
    
    
        [Fact]
        public async Task Policies_should_be_ordered_by_scope()
        {
            var policies = await _markupPolicyService.Get(AgentContext, MarkupPolicyTarget.AccommodationAvailability);
            for (var i = 0; i < policies.Count - 1; i++)
            {
                Assert.True(ScopeOrderIsCorrect(policies[i].ScopeType, policies[i + 1].ScopeType));
            }
    
            bool ScopeOrderIsCorrect(MarkupPolicyScopeType firstScope, MarkupPolicyScopeType secondScope)
            {
                switch (firstScope)
                {
                    case MarkupPolicyScopeType.Global:
                    {
                        return true;
                    }
                    case MarkupPolicyScopeType.Counterparty:
                    {
                        return secondScope != MarkupPolicyScopeType.Global;
                    }
                    case MarkupPolicyScopeType.Agency:
                    {
                        return secondScope != MarkupPolicyScopeType.Global &&
                            secondScope != MarkupPolicyScopeType.Counterparty;
                    }
                    case MarkupPolicyScopeType.Agent:
                    {
                        return secondScope != MarkupPolicyScopeType.Global &&
                            secondScope != MarkupPolicyScopeType.Counterparty &&
                            secondScope != MarkupPolicyScopeType.Agency;
                    }
                    case MarkupPolicyScopeType.EndClient:
                    {
                        return secondScope != MarkupPolicyScopeType.Global &&
                            secondScope != MarkupPolicyScopeType.Counterparty &&
                            secondScope != MarkupPolicyScopeType.Agency &&
                            secondScope != MarkupPolicyScopeType.Agent;
                    }
                    default: throw new AssertionFailedException("Unexpected scope type");
                }
            }
        }
        
        [Fact]
        public async Task Policies_in_scope_should_be_ordered_by_order()
        {
            var policies = await _markupPolicyService.Get(AgentContext, MarkupPolicyTarget.AccommodationAvailability);
            for (var i = 0; i < policies.Count - 1; i++)
            {
                Assert.True(ScopeOrderIsCorrect(policies[i], policies[i + 1]));
            }
    
            bool ScopeOrderIsCorrect(MarkupPolicy firstPolicy, MarkupPolicy secondPolicy)
            {
                if (firstPolicy.ScopeType != secondPolicy.ScopeType)
                    return true;

                if (firstPolicy.ScopeType == MarkupPolicyScopeType.Agency && 
                    secondPolicy.ScopeType == MarkupPolicyScopeType.Agency &&
                    firstPolicy.AgencyId != secondPolicy.AgencyId)
                {
                    return true;
                }
                
                return firstPolicy.Order < secondPolicy.Order;
            }
        }


        [Fact]
        public async Task Agencies_policies_should_be_ordered_by_agency_tree()
        {
            var agencyTreeIds = _agencies[0].Ancestors;
            agencyTreeIds.Add(AgentContext.AgencyId);
            var policies = await _markupPolicyService.Get(AgentContext, MarkupPolicyTarget.AccommodationAvailability);
            var agencyPolicies = policies.Where(p => p.ScopeType == MarkupPolicyScopeType.Agency).ToList();
            
            for (var i = 0; i < agencyPolicies.Count - 1; i++)
            {
                Assert.True(agencyPolicies[i].AgencyId == agencyTreeIds[i]);
            }
        }
    
        [Theory]
        [InlineData(100, Currencies.EUR, 4206528602)]
        [InlineData(240.5, Currencies.USD, 10107528602.0)]
        [InlineData(0.13, Currencies.USD, 11988602.00)]
        public async Task Policies_calculation_should_execute_in_right_order(decimal supplierPrice, Currencies currency, decimal expectedResultPrice)
        {
            var data = new TestStructureUnderMarkup {Price = new MoneyAmount(supplierPrice, currency)};
            
            var dataWithMarkup = await _markupService.ApplyMarkups(AgentContext,  data, TestStructureUnderMarkup.Apply);
            
            Assert.Equal(expectedResultPrice, dataWithMarkup.Price.Amount);
        }
    
        private readonly IEnumerable<MarkupPolicy> _agentPolicies = new[]
        {
            new MarkupPolicy
            {
                Id = 1,
                AgentId = AgentContext.AgentId,
                AgencyId = AgentContext.AgencyId,
                Order = 21,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Agent,
                TemplateId = 2,
                TemplateSettings = new Dictionary<string, decimal> {{"addition", 2}},
            },
            new MarkupPolicy
            {
                Id = 2,
                AgentId = AgentContext.AgentId,
                AgencyId = AgentContext.AgencyId,
                Order = 1,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Agent,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 2}},
            }
        };
        
        private readonly IEnumerable<MarkupPolicy> _counterpartyPolicies = new[]
        {
            new MarkupPolicy
            {
                Id = 3,
                CounterpartyId = AgentContext.CounterpartyId,
                Order = 21,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Counterparty,
                TemplateId = 2,
                TemplateSettings = new Dictionary<string, decimal> {{"addition", 32}},
            },
            new MarkupPolicy
            {
                Id = 4,
                CounterpartyId = AgentContext.CounterpartyId,
                Order = 1,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Counterparty,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 21}},
            },
        };
        
        private readonly IEnumerable<MarkupPolicy> _globalPolicies = new[]
        {
            new MarkupPolicy
            {
                Id = 5,
                Order = 23,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Global,
                TemplateId = 2,
                TemplateSettings = new Dictionary<string, decimal> {{"addition", 14}},
            },
            new MarkupPolicy
            {
                Id = 6,
                Order = 1,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Global,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 100}},
            }
        };
        
        private readonly IEnumerable<MarkupPolicy> _agencyPolicies = new[]
        {
            new MarkupPolicy
            {
                Id = 7,
                Order = 1,
                AgencyId = AgentContext.AgencyId,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Agency,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 100}},
            },
            new MarkupPolicy
            {
                Id = 10,
                Order = 1,
                AgencyId = 1000,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Agency,
                TemplateId = 2,
                TemplateSettings = new Dictionary<string, decimal> {{"addition", 43}},
            },
            new MarkupPolicy
            {
                Id = 11,
                Order = 1,
                AgencyId = 2000,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.Agency,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 100}},
            },
        };
        
        private IEnumerable<MarkupPolicy> _endClientPolicies = new[]
        {
            new MarkupPolicy
            {
                Id = 8,
                Order = 111,
                AgentId = AgentContext.AgentId,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.EndClient,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 100}},
            },
            new MarkupPolicy
            {
                Id = 9,
                Order = 14,
                AgentId = AgentContext.AgentId,
                Target = MarkupPolicyTarget.AccommodationAvailability,
                ScopeType = MarkupPolicyScopeType.EndClient,
                TemplateId = 1,
                TemplateSettings = new Dictionary<string, decimal> {{"factor", 14}},
            }
        };
        
        private readonly List<Agency> _agencies = new()
        {
            new Agency
            {
                Id = AgentContext.AgencyId,
                Name = "Child agency",
                Ancestors = new List<int> {2000, 1000}
            },
            new Agency
            {
                Id = 1000,
                Name = "Parent agency",
                Ancestors = new List<int>{2000}
            },
            new Agency
            {
                Id = 2000,
                Name = "Root agency",
                Ancestors = new()
            }
        };

        public void Dispose() { }


        private static readonly AgentContext AgentContext = AgentContextFactory.CreateWithCounterpartyAndAgency(1, 1, 1);
        private readonly MarkupPolicyService _markupPolicyService;
        private readonly MarkupService _markupService;
    }
}