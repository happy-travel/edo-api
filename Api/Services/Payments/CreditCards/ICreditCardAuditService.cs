using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Money.Enums;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCards
{
    public interface ICreditCardAuditService
    {
        Task Write<TEventData>(CreditCardEventType eventType, string maskedNumber, decimal amount, ApiCaller apiCaller, TEventData eventData, string referenceCode,
            int agentId, Currencies currency);
    }
}