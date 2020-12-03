using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace HappyTravel.Edo.Api.Services.Markups
{
    public interface IPaymentMarkupService
    {
        Task<Result> Pay(int bookingId);

        Task<Result> Refund(int bookingId);
    }
}