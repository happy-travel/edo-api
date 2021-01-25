using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Locations;

namespace HappyTravel.Edo.Api.Infrastructure.Locations
{
    public interface ILocationClient
    {
        Task<Result<Location>> Get(string id, string languageCode, CancellationToken cancellationToken = default);
        Task<Result<List<Location>>> Search(string query, string languageCode, int skip = 0, int top = 10, CancellationToken cancellationToken = default);
    }
}