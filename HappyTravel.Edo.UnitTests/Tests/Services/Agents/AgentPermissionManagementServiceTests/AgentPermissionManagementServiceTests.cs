using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Extensions;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using HappyTravel.Edo.UnitTests.Utility;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Tests.Services.Agents.AgentPermissionManagementServiceTests
{
    public class AgentPermissionManagementServiceTests
    {
        public AgentPermissionManagementServiceTests()
        {
            var edoContextMock = MockEdoContextFactory.Create();
            edoContextMock.Setup(x => x.AgentAgencyRelations).Returns(DbSetMockProvider.GetDbSetMock(_relations));

            _agentPermissionManagementService = new AgentPermissionManagementService(edoContextMock.Object);
        }


        [Fact]
        public async Task Set_must_succeed()
        {
            var (isSuccess, _, _, _) = await _agentPermissionManagementService
                .SetInAgencyPermissions(1, InAgencyPermissions.None.ToList(), AgentContextRegular);

            Assert.True(isSuccess);
        }


        [Fact]
        public async Task Set_relation_not_found_must_fail()
        {
            var (_, isFailure, _, error) = await _agentPermissionManagementService
                .SetInAgencyPermissions(0, InAgencyPermissions.None.ToList(), AgentContextRegular);

            Assert.True(isFailure);
            Assert.Equal("Could not find relation between the agent 0 and the agency 1", error);
        }


        [Fact]
        public async Task Set_revoke_last_management_must_fail()
        {
            var (_, isFailure, _, error) = await _agentPermissionManagementService
                .SetInAgencyPermissions(2, InAgencyPermissions.None.ToList(), AgentContextRegular);

            Assert.True(isFailure);
            Assert.Equal("Cannot revoke last permission management rights", error);
        }
        

        private readonly IEnumerable<AgentAgencyRelation> _relations = new[]
        {
            new AgentAgencyRelation
            {
                AgencyId = 1,
                AgentId = 1,
                Type = AgentAgencyRelationTypes.Master,
                InAgencyPermissions = InAgencyPermissions.AgentInvitation
            },
            new AgentAgencyRelation
            {
                AgencyId = 1,
                AgentId = 2,
                Type = AgentAgencyRelationTypes.Regular,
                InAgencyPermissions = InAgencyPermissions.PermissionManagement
            }
        };

        
        private static readonly AgentContext AgentContextRegular = AgentInfoFactory.CreateWithCounterpartyAndAgency(10, 1, 1);

        private readonly AgentPermissionManagementService _agentPermissionManagementService;
    }
}