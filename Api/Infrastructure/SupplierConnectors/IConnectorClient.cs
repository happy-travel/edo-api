using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Infrastructure.SupplierConnectors
{
    public interface IConnectorClient
    {
        Task<Result<T, ProblemDetails>> Get<T>(Uri url, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default);


        Task<Result<TOut, ProblemDetails>> Post<T, TOut>(Uri url, T requestContent, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default);


        Task<Result<TOut, ProblemDetails>> Post<TOut>(Uri url, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default);
        
        
        Task<Result<Unit, ProblemDetails>> Post(Uri uri,
            string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default);

        
        Task<Result<TOut, ProblemDetails>> Send<TOut>(Func<HttpRequestMessage> requestFactory,
            string languageCode = LocalizationHelper.DefaultLanguageCode, CancellationToken cancellationToken = default);


        public Task<Result<TOut, ProblemDetails>> Post<TOut>(Uri url, Stream stream, string languageCode = LocalizationHelper.DefaultLanguageCode,
            CancellationToken cancellationToken = default);
    }
}