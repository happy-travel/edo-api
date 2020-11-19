using System.Threading.Tasks;
using HappyTravel.Edo.Api.Models.Users;

namespace HappyTravel.Edo.Api.Services.Payments.CreditCardConfirmation
{
    public interface ICreditCardPaymentConfirmationAuditService
    {
        Task Write(UserInfo user, string referenceCode);
    }
}