using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace HappyTravel.Edo.Api.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequestRateLimitAttribute : ActionFilterAttribute
    {
        public RequestRateLimitAttribute(int seconds)
        {
            Seconds = seconds;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var flow = context.HttpContext.RequestServices.GetRequiredService<IMemoryFlow>();
            
            var key = flow.BuildKey(nameof(RequestRateLimitAttribute),
                context.HttpContext.Connection.RemoteIpAddress.ToString(),
                context.HttpContext.Request.Path.Value);
            
            if (flow.TryGetValue<bool>(key, out _))
            {
                context.HttpContext.Response.StatusCode = (int) HttpStatusCode.TooManyRequests;
                context.HttpContext.Response.Headers.Add(HeaderNames.RetryAfter, Seconds.ToString());
                context.Result = new EmptyResult();
            }
            else
            {
                flow.Set(key, true, TimeSpan.FromSeconds(Seconds));
            }
        }
        
        public int Seconds { get; }
    }
}