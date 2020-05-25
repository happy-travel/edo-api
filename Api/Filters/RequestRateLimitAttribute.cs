using System;
using System.Net;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace HappyTravel.Edo.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestRateLimitAttribute : ActionFilterAttribute
    {
        public RequestRateLimitAttribute(int milliseconds)
        {
            Milliseconds = milliseconds;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var flow = context.HttpContext.RequestServices.GetRequiredService<IMemoryFlow>();
            
            var key = flow.BuildKey(context.HttpContext.Connection.RemoteIpAddress.ToString(),
                context.HttpContext.Request.Path.Value);
            
            if (flow.TryGetValue<bool>(key, out _))
            {
                context.Result = new ContentResult { Content =  $"Requests are limited to 1 request each {Milliseconds} ms."};
                context.HttpContext.Response.StatusCode = (int) HttpStatusCode.TooManyRequests;
            }
            else
            {
                flow.Set(key, true, TimeSpan.FromMilliseconds(Milliseconds));
            }
        }
        
        public int Milliseconds { get; }
    }
}