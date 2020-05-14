using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Infrastructure;

namespace HappyTravel.Edo.Api.Services.ProviderResponses
{
    public interface INetstormingResponseService
    {
        Task<Result> ProcessBookingDetailsResponse(byte[] xmlRequestData, RequestMetadata requestMetadata);
    }
}