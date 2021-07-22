using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using CacheFlow.Json.Extensions;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Conventions;
using HappyTravel.Edo.Api.Filters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Environments;
using HappyTravel.Edo.Api.Models.Storage;
using HappyTravel.Edo.Api.NotificationCenter.Hubs;
using HappyTravel.Edo.Api.NotificationCenter.Infrastructure;
using HappyTravel.Edo.Api.Services.Accommodations.Availability;
using HappyTravel.Edo.Api.Services.Hubs.Search;
using HappyTravel.Edo.Data;
using HappyTravel.ErrorHandling.Extensions;
using HappyTravel.Telemetry.Extensions;
using HappyTravel.VaultClient;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Prometheus;
using StackExchange.Redis;

namespace HappyTravel.Edo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }


        public void ConfigureServices(IServiceCollection services)
        {
            var serializationSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };
            JsonConvert.DefaultSettings = () => serializationSettings;

            using var vaultClient = new VaultClient.VaultClient(new VaultOptions
            {
                BaseUrl = new Uri(EnvironmentVariableHelper.Get("Vault:Endpoint", Configuration)),
                Engine = Configuration["Vault:Engine"],
                Role = Configuration["Vault:Role"]
            });
            vaultClient.Login(EnvironmentVariableHelper.Get("Vault:Token", Configuration)).GetAwaiter().GetResult();

            services.AddResponseCompression()
                .AddCors()
                .AddLocalization()
                .AddMemoryCache()
                .AddMemoryFlow()
                .AddStackExchangeRedisCache(options =>
                {
                    var host = EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration);
                    options.ConfigurationOptions = new ConfigurationOptions
                    {
                        EndPoints = {new DnsEndPoint(host, 6379)},
                        AsyncTimeout = 15000, // set to 15 seconds before we stop storing large objects in Redis
                    };
                })
                .AddDoubleFlow()
                .AddCacheFlowJsonSerialization()
                .AddTracing(Configuration, options =>
                {
                    options.ServiceName = $"{HostingEnvironment.ApplicationName}-{HostingEnvironment.EnvironmentName}";
                    options.JaegerHost = HostingEnvironment.IsLocal()
                        ? Configuration.GetValue<string>("Jaeger:AgentHost")
                        : Configuration.GetValue<string>(Configuration.GetValue<string>("Jaeger:AgentHost"));
                    options.JaegerPort = HostingEnvironment.IsLocal()
                        ? Configuration.GetValue<int>("Jaeger:AgentPort")
                        : Configuration.GetValue<int>(Configuration.GetValue<string>("Jaeger:AgentPort"));
                    options.RedisEndpoint = Configuration.GetValue<string>(Configuration.GetValue<string>("Redis:Endpoint"));
                })
                .AddUserEventLogging(Configuration, vaultClient);

            var (apiName, authorityUrl) = GetApiNameAndAuthority(Configuration, HostingEnvironment, vaultClient);

            services.ConfigureServiceOptions(Configuration, HostingEnvironment, vaultClient)
                .ConfigureHttpClients(Configuration, HostingEnvironment, vaultClient, authorityUrl)
                .ConfigureAuthentication(Configuration, HostingEnvironment, apiName, authorityUrl)
                .AddServices();

            services.AddHealthChecks()
                .AddDbContextCheck<EdoContext>()
                .AddRedis(EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration))
                .AddCheck<ControllerResolveHealthCheck>(nameof(ControllerResolveHealthCheck));
            
            services.AddProblemDetailsFactory();
            
            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("agent", new OpenApiInfo { Title = "Happytravel.com Edo API for an agent app", Version = "v1.0" });
                options.SwaggerDoc("admin", new OpenApiInfo { Title = "Happytravel.com Edo API for an admin app", Version = "v1.0" });
                options.SwaggerDoc("property-owner", new OpenApiInfo { Title = "Happytravel.com Edo API for property owners", Version = "v1.0" });
                options.SwaggerDoc("service", new OpenApiInfo { Title = "Happytravel.com service Edo API", Version = "v1.0" });

                var xmlCommentsFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlCommentsFilePath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFileName);
                options.IncludeXmlComments(xmlCommentsFilePath);
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        Array.Empty<string>()
                    }
                });
                // Use fully qualified object names
                options.CustomSchemaIds(x => x.FullName);
            });
            services.AddSwaggerGenNewtonsoftSupport();
            
            services.AddOData();
            services.AddNotificationCenter();

            services.Configure<AvailabilityStorageSettings>(o =>
            {
                o.ConnectionString = Configuration.GetValue<string>(Configuration.GetValue<string>("MongoDB:ConnectionString"));
                o.CollectionName = nameof(AvailabilityStorage);
                o.DatabaseName = Configuration.GetValue<string>("MongoDB:Database");
            });

            services.AddSingleton<IAvailabilityStorage, AvailabilityStorage>();
            
            services.AddMvcCore(options =>
                {
                    options.Conventions.Insert(0, new LocalizationConvention());
                    options.Conventions.Add(new AuthorizeControllerModelConvention());
                    options.Conventions.Add(new ApiExplorerGroupPerVersionConvention());
                    options.Filters.Add(new MiddlewareFilterAttribute(typeof(LocalizationPipelineFilter)));
                    options.Filters.Add(typeof(ModelValidationFilter));
                    
                    AddODataMediaTypes(options);
                })
                .AddAuthorization()
                .AddControllersAsServices()
                .AddMvcOptions(m => m.EnableEndpointRouting = true)
                .AddFormatterMappings()
                .AddNewtonsoftJson(options => options.SerializerSettings.Converters.Add(new StringEnumConverter()))
                .AddApiExplorer()
                .AddCacheTagHelper()
                .AddDataAnnotations();
        }


        /// <remarks>
        /// This is a workaround to make OData work with swagger: https://github.com/OData/WebApi/issues/1177
        /// </remarks>
        private static void AddODataMediaTypes(MvcOptions options)
        {
            foreach (var outputFormatter in options.OutputFormatters.OfType<ODataOutputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
            {
                outputFormatter.SupportedMediaTypes
                    .Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
            }

            foreach (var inputFormatter in options.InputFormatters.OfType<ODataInputFormatter>().Where(_ => _.SupportedMediaTypes.Count == 0))
            {
                inputFormatter.SupportedMediaTypes
                    .Add(new MediaTypeHeaderValue("application/prs.odatatestxx-odata"));
            }
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            Infrastructure.Logging.AppLogging.LoggerFactory = loggerFactory;
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/robots.txt"))
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("User-agent: * \nDisallow: /");
                }
                else
                {
                    await next();
                }
            });

            var logger = loggerFactory.CreateLogger<Startup>();
            app.UseProblemDetailsExceptionHandler(env, logger);

            app.UseSwagger()
                .UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/agent/swagger.json", "Happytravel.com Edo API for an agent app");
                    options.SwaggerEndpoint("/swagger/admin/swagger.json", "Happytravel.com Edo API for an admin app");
                    options.SwaggerEndpoint("/swagger/property-owner/swagger.json", "Happytravel.com Edo API for property owners");
                    options.SwaggerEndpoint("/swagger/service/swagger.json", "Happytravel.com service Edo API");
                    options.RoutePrefix = string.Empty;
                });

            app.UseResponseCompression()
                .UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());

            var headersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                RequireHeaderSymmetry = false,
                ForwardLimit = null
            };
            headersOptions.KnownNetworks.Clear();
            headersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(headersOptions);

            app.UseHealthChecks("/health");
            app.UseRouting()
                .UseHttpMetrics()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapMetrics();
                    endpoints.MapControllers();
                    endpoints.EnableDependencyInjection();
                    endpoints.Filter(QueryOptionSetting.Allowed).OrderBy().Expand().Select().MaxTop(100);
                    endpoints.MapHub<AgentNotificationHub>("/signalr/notifications/agents");
                    endpoints.MapHub<AdminNotificationHub>("/signalr/notifications/admins");
                    endpoints.MapHub<SearchHub>("/signalr/search");
                });
        }


        private static (string apiName, string authorityUrl) GetApiNameAndAuthority(IConfiguration configuration, IWebHostEnvironment environment,
            IVaultClient vaultClient)
        {
            var authorityOptions = vaultClient.Get(configuration["Authority:Options"]).GetAwaiter().GetResult();

            var apiName = configuration["Authority:ApiName"];
            var authorityUrl = configuration["Authority:Endpoint"];
            if (environment.IsDevelopment() || environment.IsLocal())
                return (apiName, authorityUrl);

            apiName = authorityOptions["apiName"];
            authorityUrl = authorityOptions["authorityUrl"];

            return (apiName, authorityUrl);
        }


        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }
    }
}