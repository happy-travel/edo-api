using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CacheFlow.Json.Extensions;
using FloxDc.Bento.Responses.Middleware;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Conventions;
using HappyTravel.Edo.Api.Filters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Environments;
using HappyTravel.Edo.Data;
using HappyTravel.StdOutLogger.Extensions;
using HappyTravel.VaultClient;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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
                .AddStackExchangeRedisCache(options => { options.Configuration = EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration); })
                .AddDoubleFlow()
                .AddCashFlowJsonSerialization();
                //.AddTracing(HostingEnvironment, Configuration);
            
            services.ConfigureServiceOptions(Configuration, HostingEnvironment, vaultClient)
                .ConfigureHttpClients(Configuration, HostingEnvironment, vaultClient)
                .ConfigureAuthentication(Configuration, HostingEnvironment, vaultClient)
                .AddServices();

            services.AddHealthChecks()
                .AddDbContextCheck<EdoContext>()
                .AddRedis(EnvironmentVariableHelper.Get("Redis:Endpoint", Configuration))
                .AddCheck<ControllerResolveHealthCheck>(nameof(ControllerResolveHealthCheck));

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1.0", new OpenApiInfo {Title = "HappyTravel.com Edo API", Version = "v1.0"});

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
            });
            services.AddSwaggerGenNewtonsoftSupport();
            
            services.AddMvcCore(options =>
                {
                    options.Conventions.Insert(0, new LocalizationConvention());
                    options.Conventions.Add(new AuthorizeControllerModelConvention());
                    options.Filters.Add(new MiddlewareFilterAttribute(typeof(LocalizationPipelineFilter)));
                    options.Filters.Add(typeof(ModelValidationFilter));
                })
                .AddAuthorization()
                .AddControllersAsServices()
                .AddMvcOptions(m => m.EnableEndpointRouting = true)
                .AddFormatterMappings()
                .AddNewtonsoftJson(options => options.SerializerSettings.Converters.Add(new StringEnumConverter()))
                .AddApiExplorer()
                .AddCacheTagHelper()
                .AddDataAnnotations();
            
            services.AddOData();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            Infrastructure.Logging.AppLogging.LoggerFactory = loggerFactory;
            app.UseBentoExceptionHandler(env.IsProduction());
            app.UseHttpContextLogging(
                options => options.IgnoredPaths = new HashSet<string> {"/health", "/locations"}
            );

            app.UseSwagger()
                .UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "HappyTravel.com Edo API");
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
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.EnableDependencyInjection();
                    endpoints.Filter().OrderBy().Expand().Select().MaxTop(500);
                });
        }


        public IConfiguration Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }
    }
}