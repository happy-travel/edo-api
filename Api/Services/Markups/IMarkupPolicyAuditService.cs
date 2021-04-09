using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IMarkupPolicyAuditService
    {
        Task Write<TEventData>(MarkupPolicyEventType eventType, TEventData eventData, ApiCaller apiCaller);
    }
}
