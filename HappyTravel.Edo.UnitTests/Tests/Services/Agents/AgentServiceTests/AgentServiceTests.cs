using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.Data.Markup;
using HappyTravel.Edo.UnitTests.Utility;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Agents.AgentServiceTests
{
    public class AgentServiceTests
    {
        public AgentServiceTests()
        {
            var edoContextMock = MockEdoContextFactory.Create();
            edoContextMock.Setup(x => x.Counterparties).Returns(DbSetMockProvider.GetDbSetMock(_counterparties));
            edoContextMock.Setup(x => x.Agencies).Returns(DbSetMockProvider.GetDbSetMock(_agencies));
            edoContextMock.Setup(x => x.Agents).Returns(DbSetMockProvider.GetDbSetMock(_agents));
            edoContextMock.Setup(x => x.AgentAgencyRelations).Returns(DbSetMockProvider.GetDbSetMock(_relations));
            edoContextMock.Setup(x => x.MarkupPolicies).Returns(DbSetMockProvider.GetDbSetMock(new List<MarkupPolicy>()));

            _agentService = new AgentService(edoContextMock.Object, new DefaultDateTimeProvider(), new MarkupPolicyTemplateService());
        }

        [Fact]
        public async Task Agency_mismatch_must_fail_get_agent()
        {
            var (_, isFailure, _, _) = await _agentService.GetAgent( 4, AgentContext);
            Assert.True(isFailure);
        }


        [Fact]
        public async Task Not_found_agent_must_fail()
        {
            var (_, isFailure, _, _) = await _agentService.GetAgent( 0, AgentContext);
            Assert.True(isFailure);
        }

        [Fact]
        public async Task Found_agent_must_match()
        {
            var expectedAgent = new AgentInfoInAgency(1, "fn", "ln", "email", "title", "pos", 1, "comName",
                1, "agencyName", true, InAgencyPermissions.ObserveMarkup.ToList(), true);

            var (isSuccess, _, actualAgent, _) = await _agentService.GetAgent(1, AgentContext);

            Assert.True(isSuccess);

            Assert.Equal(expectedAgent.AgentId, actualAgent.AgentId);
            Assert.Equal(expectedAgent.FirstName, actualAgent.FirstName);
            Assert.Equal(expectedAgent.LastName, actualAgent.LastName);
            Assert.Equal(expectedAgent.Email, actualAgent.Email);
            Assert.Equal(expectedAgent.Title, actualAgent.Title);
            Assert.Equal(expectedAgent.Position, actualAgent.Position);
            Assert.Equal(expectedAgent.CounterpartyId, actualAgent.CounterpartyId);
            Assert.Equal(expectedAgent.CounterpartyName, actualAgent.CounterpartyName);
            Assert.Equal(expectedAgent.AgencyId, actualAgent.AgencyId);
            Assert.Equal(expectedAgent.AgencyName, actualAgent.AgencyName);
            Assert.Equal(expectedAgent.IsMaster, actualAgent.IsMaster);
            Assert.Equal(expectedAgent.InAgencyPermissions, actualAgent.InAgencyPermissions);
        }

        [Fact]
        public async Task Found_agent_list_must_match()
        {
            var expectedAgents = new List<SlimAgentInfo>
            {
                new SlimAgentInfo(1, "fn", "ln", default, string.Empty, true),
                new SlimAgentInfo(2, "fn2", "ln2", default, string.Empty, true)
            };

            var actualAgents = await _agentService.GetAgents(AgentContext).ToListAsync();
            Assert.Equal(expectedAgents, actualAgents);
        }

        [Fact]
        public async Task Edit_agent_should_change_fields()
        {
            var newInfo = new AgentEditableInfo("newTitle", "newFn", "newLn", "newPos", string.Empty);
            var changedAgent = _agents.Single(a => a.Id == AgentContext.AgentId);
            var expectedValues = new[] {"newTitle", "newFn", "newLn", "newPos"};

            await _agentService.UpdateCurrentAgent(newInfo, AgentContext);

            Assert.Equal(expectedValues,
                new []
                {
                    changedAgent.Title,
                    changedAgent.FirstName,
                    changedAgent.LastName,
                    changedAgent.Position
                });
        }

        private readonly IEnumerable<Agent> _agents = new []
        {
            new Agent
            {
                Id = 1,
                Email = "email",
                FirstName = "fn",
                LastName = "ln",
                Position = "pos",
                Title = "title"
            },
            new Agent
            {
                Id = 2,
                Email = "email2",
                FirstName = "fn2",
                LastName = "ln2",
                Position = "pos2",
                Title = "title2"
            },
            new Agent
            {
                Id = 3,
                Email = "email3",
                FirstName = "fn3",
                LastName = "ln3",
                Position = "pos3",
                Title = "title3"
            },
            new Agent
            {
                Id = 4,
                Email = "email4",
                FirstName = "fn4",
                LastName = "ln4",
                Position = "pos4",
                Title = "title4"
            },
        };

        private readonly IEnumerable<Counterparty> _counterparties = new[]
        {
            new Counterparty
            {
                Id = 1,
                Name = "comName",
                State = CounterpartyStates.FullAccess
            }
        };

        private readonly IEnumerable<Agency> _agencies = new[]
        {
            new Agency
            {
                Id = 1,
                CounterpartyId = 1,
                Name = "agencyName",
                ParentId = null,
            },
            new Agency
            {
                Id = 2,
                CounterpartyId = 1,
                Name = "agencyName2",
                ParentId = 1,
            }
        };

        private readonly IEnumerable<AgentAgencyRelation> _relations = new[]
        {
            new AgentAgencyRelation
            {
                AgencyId = 1,
                AgentId = 1,
                Type = AgentAgencyRelationTypes.Master,
                InAgencyPermissions = InAgencyPermissions.ObserveMarkup,
                IsActive = true
            },
            new AgentAgencyRelation
            {
                AgencyId = 1,
                AgentId = 2,
                Type = AgentAgencyRelationTypes.Regular,
                IsActive = true
            },
            new AgentAgencyRelation
            {
                AgencyId = 2,
                AgentId = 4,
                Type = AgentAgencyRelationTypes.Regular,
                IsActive = true
            }
        };

        private static readonly AgentContext AgentContext = AgentInfoFactory.CreateWithCounterpartyAndAgency(3, 1, 1);
        private readonly AgentService _agentService;
    }
}
