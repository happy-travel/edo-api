using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using HappyTravel.Edo.Api.Models.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Infrastructure.DataProviders
{
    public interface IDataProviderClient
    {
        Task<Result<T, ProblemDetails>> Get<T>(Uri url, RequestMetadata requestMetadata,
            CancellationToken cancellationToken = default);


        Task<Result<TOut, ProblemDetails>> Post<T, TOut>(Uri url, T requestContent, RequestMetadata requestMetadata,
            CancellationToken cancellationToken = default);


        Task<Result<TOut, ProblemDetails>> Post<TOut>(Uri url, RequestMetadata requestMetadata,
            CancellationToken cancellationToken = default);
        
        
        Task<Result<VoidObject, ProblemDetails>> Post(Uri uri,
            RequestMetadata requestMetadata,
            CancellationToken cancellationToken = default);

        
        Task<Result<TOut, ProblemDetails>> Send<TOut>(HttpRequestMessage httpRequestMessage,
            RequestMetadata requestMetadata, CancellationToken cancellationToken = default);


        public Task<Result<TOut, ProblemDetails>> Post<TOut>(Uri url, Stream stream, RequestMetadata requestMetadata,
            CancellationToken cancellationToken = default);
    }
}