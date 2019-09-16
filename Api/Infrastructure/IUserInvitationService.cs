using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Infrastructure
{
    public interface IUserInvitationService
    {
        Task<Result> Send<TInvitationData>(string email, TInvitationData invitationInfo, string mailTemplateId, UserInvitationTypes invitationType);
        Task Accept(string invitationCode);
        Task<Result<TInvitationData>> GetPendingInvitation<TInvitationData>(string invitationCode, UserInvitationTypes invitationType);
    }
}