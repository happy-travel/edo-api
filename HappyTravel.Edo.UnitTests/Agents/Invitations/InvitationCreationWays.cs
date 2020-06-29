using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;
using HappyTravel.Edo.Api.Models.Mailing;
using HappyTravel.Edo.Api.Services.Agents;
using HappyTravel.Edo.Api.Services.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.UnitTests.Infrastructure;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HappyTravel.Edo.UnitTests.Agents.Invitations
{
    public class InvitationCreationWays
    {
        public InvitationCreationWays()
        {
            var agent = AgentInfoFactory.CreateByWithCounterpartyAndAgency(It.IsAny<int>(), It.IsAny<int>(), AgentAgencyId);
            var agentContext = new Mock<IAgentContextService>();
            agentContext
                .Setup(c => c.GetAgent())
                .ReturnsAsync(agent);

            _userInvitationService = new FakeUserInvitationService();
            var counterpartyServiceMock = new Mock<ICounterpartyService>();

            counterpartyServiceMock
                .Setup(c => c.Get(It.IsAny<int>()))
                .ReturnsAsync(Result.Ok(FakeCounterpartyInfo));

            counterpartyServiceMock
                .Setup(c => c.GetAgency(It.IsAny<int>()))
                .ReturnsAsync(Result.Ok(FakeAgencyInfo));

            var optionsMock = new Mock<IOptions<AgentInvitationOptions>>();
            optionsMock.Setup(o => o.Value).Returns(new AgentInvitationOptions
            {
                EdoPublicUrl = It.IsAny<string>(),
                MailTemplateId = It.IsAny<string>()
            });

            _invitationService = new AgentInvitationService(agentContext.Object,
                optionsMock.Object,
                _userInvitationService,
                counterpartyServiceMock.Object);
        }


        [Fact]
        public async Task Different_ways_should_create_same_invitations()
        {
            var invitationInfo = new AgentInvitationInfo(It.IsAny<AgentEditableInfo>(),
                AgentAgencyId, It.IsAny<string>());

            await _invitationService.Send(invitationInfo);
            await _invitationService.Create(invitationInfo);

            Assert.Equal(_userInvitationService.CreatedInvitationInfo.GetType(), _userInvitationService.SentInvitationInfo.GetType());
            Assert.Equal(_userInvitationService.CreatedInvitationInfo, _userInvitationService.SentInvitationInfo);
        }
        
        
        private readonly AgentInvitationService _invitationService;
        private const int AgentAgencyId = 123;

        private static readonly CounterpartyInfo FakeCounterpartyInfo =
            new CounterpartyInfo("SomeName", default, default, default, default, default, default, default, default, default, default, default);

        private static readonly AgencyInfo FakeAgencyInfo =
            new AgencyInfo("SomeAgencyName", default);

        private readonly FakeUserInvitationService _userInvitationService;
    }

    public class FakeUserInvitationService : IUserInvitationService
    {
        public Task<Result> Send<TInvitationData>(string email, TInvitationData invitationInfo,
            Func<TInvitationData, string, DataWithCompanyInfo> messagePayloadGenerator, string mailTemplateId,
            UserInvitationTypes invitationType)
        {
            SentInvitationInfo = invitationInfo;
            return Task.FromResult(Result.Ok());
        }


        public Task<Result<string>> Create<TInvitationData>(string email, TInvitationData invitationInfo, UserInvitationTypes invitationType)
        {
            CreatedInvitationInfo = invitationInfo;
            return Task.FromResult(Result.Ok(string.Empty));
        }


        public Task Accept(string invitationCode) => throw new NotImplementedException();


        public Task<Result<TInvitationData>> GetPendingInvitation<TInvitationData>(string invitationCode, UserInvitationTypes invitationType)
            => throw new NotImplementedException();


        public object SentInvitationInfo { get; set; }

        public object CreatedInvitationInfo { get; set; }
    }
}