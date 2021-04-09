using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.Payments.Accounts
{
    public interface IAccountBalanceAuditService
    {
        Task Write<TEventData>(AccountEventType eventType, int accountId, decimal amount, ApiCaller apiCaller, TEventData eventData, string referenceCode);
    }
}