using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;

namespace HappyTravel.Edo.Api.Services.Payments.Offline
{
    public interface IOfflinePaymentAuditService
    {
        Task Write(ApiCaller apiCaller, string referenceCode);
    }
}
