using FloxDc.CacheFlow;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Converters;
using HappyTravel.Edo.UnitTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Xunit.DependencyInjection;

[assembly: TestFramework("HappyTravel.Edo.UnitTests.Startup", "HappyTravel.Edo.UnitTests")]

namespace HappyTravel.Edo.UnitTests
{
    public class Startup : DependencyInjectionTestFramework
    {
        public Startup(IMessageSink messageSink) : base(messageSink)
        { }


        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddSingleton<IMemoryFlow, FakeMemoryFlow>();
            services.AddTransient(provider => MockEdoContext.Create());
            services.AddSingleton<IDateTimeProvider, DefaultDateTimeProvider>();
        }
    }
}
