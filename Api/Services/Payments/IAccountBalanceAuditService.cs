using System.Threading.Tasks;
using HappyTravel.Edo.Api.Infrastructure.Users;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Payments
{
    public interface IAccountBalanceAuditService
    {
        Task Write<TEventData>(AccountEventType eventType, int accountId, decimal amount, UserInfo user, TEventData eventData, string referenceCode);
    }
}