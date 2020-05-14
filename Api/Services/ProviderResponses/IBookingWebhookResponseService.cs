using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using HappyTravel.Edo.Common.Enums;

namespace HappyTravel.Edo.Api.Services.ProviderResponses
{
    public interface IBookingWebhookResponseService
    {
        Task<Result> ProcessBookingData(Stream requestBody, DataProviders dataProvider, RequestMetadata requestMetadata);
    }
}