using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.Edo.Api.Infrastructure.FunctionalExtensions
{
    public static class ProblemDetailsConversionExtensions
    {
        public static async Task<Result<T, ProblemDetails>> ToResultWithProblemDetails<T>(this Task<Result<T>> task)
        {
            var (isSuccess, _, result, error) = await task;

            return isSuccess
                ? Result.Success<T, ProblemDetails>(result)
                : ProblemDetailsBuilder.Fail<T>(error);
        }
        
        
        public static async Task<Result<Unit, ProblemDetails>> ToResultWithProblemDetails(this Task<Result> task)
        {
            var (isSuccess, _, error) = await task;

            return isSuccess
                ? Result.Success<Unit, ProblemDetails>(Unit.Instance)
                : ProblemDetailsBuilder.Fail<Unit>(error);
        }
    }
}