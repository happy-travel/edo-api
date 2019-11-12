using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using FloxDc.Bento.Responses.Middleware;
using FloxDc.CacheFlow;
using FloxDc.CacheFlow.Extensions;
using HappyTravel.Edo.Api.Conventions;
using HappyTravel.Edo.Api.Filters;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Constants;
using HappyTravel.Edo.Api.Infrastructure.Converters;
using HappyTravel.Edo.Api.Infrastructure.Emails;
using HappyTravel.Edo.Api.Infrastructure.Environments;
using HappyTravel.Edo.Api.Models.Management;
using HappyTravel.Edo.Api.Services.Accommodations;
using HappyTravel.Edo.Api.Services.CodeGeneration;
using HappyTravel.Edo.Api.Services.CurrencyConversion;
using HappyTravel.Edo.Api.Services.Customers;
using HappyTravel.Edo.Api.Services.Deadline;
using HappyTravel.Edo.Api.Services.Locations;
using HappyTravel.Edo.Api.Services.Management;
using HappyTravel.Edo.Api.Services.Markups;
using HappyTravel.Edo.Api.Services.Markups.Availability;
using HappyTravel.Edo.Api.Services.Markups.Templates;
using HappyTravel.Edo.Api.Services.PaymentLinks;
using HappyTravel.Edo.Api.Services.Payments;
using HappyTravel.Edo.Api.Services.SupplierOrders;
using HappyTravel.Edo.Common.Enums;
using HappyTravel.Edo.Data;
using HappyTravel.StdOutLogger.Extensions;
using HappyTravel.VaultClient;
using HappyTravel.VaultClient.Extensions;
using IdentityModel.Client;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetTopologySuite;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Polly;
using Polly.Extensions.Http;
using SendGrid.Helpers.Mail;
using Swashbuckle.AspNetCore.Swagger;

namespace HappyTravel.Edo.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }


        public IConfiguration Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();

            var serializationSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };
            JsonConvert.DefaultSettings = () => serializationSettings;
            
            services.AddMvcCore(options =>
                {
                    options.Conventions.Insert(0, new LocalizationConvention());
                    options.Conventions.Add(new AuthorizeControllerModelConvention());
                    options.Filters.Add(new MiddlewareFilterAttribute(typeof(LocalizationPipeline)));
                    options.Filters.Add(typeof(ModelValidation));
                })
                .AddAuthorization()
                .AddCors()
                .AddControllersAsServices()
                .AddFormatterMappings()
                .AddJsonFormatters()
                .AddApiExplorer()
                .AddCacheTagHelper()
                .AddDataAnnotations()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddCors()
                .AddLocalization()
                .AddMemoryCache()
                .AddMemoryFlow();

            services.AddVaultClient(o =>
            {
                o.Engine = Configuration["Vault:Engine"];
                o.Role = Configuration["Vault:Role"];
                o.Url = new Uri(EnvironmentVariableHelper.Get("Vault:Endpoint", Configuration));
            });
            
            Dictionary<string, string> authorityOptions = null;
            Dictionary<string, string> dataProvidersOptions = null;
            Dictionary<string, string> paymentLinksOptions = null;

            Dictionary<string, string> databaseOptions;
            Dictionary<string, string> googleOptions;
            Dictionary<string, string> payfortOptions;
            Dictionary<string, string> payfortUrlsOptions;
            string sendGridApiKey;
            string senderAddress;
            string customerInvitationTemplateId;
            string administratorInvitationTemplateId;
            string externalPaymentsMailTemplateId;
            
            var serviceProvider = services.BuildServiceProvider();
            using (var vaultClient = serviceProvider.GetService<IVaultClient>())
            {
                vaultClient.Login(EnvironmentVariableHelper.Get("Vault:Token", Configuration)).Wait();

                databaseOptions = vaultClient.Get(Configuration["Edo:Database:Options"]).Result;
                googleOptions = vaultClient.Get(Configuration["Edo:Google:Options"]).Result;
                payfortOptions = vaultClient.Get(Configuration["Edo:Payfort:Options"]).Result;
                payfortUrlsOptions = vaultClient.Get(Configuration["Edo:Payfort:Urls"]).Result;
                var mailSettings = vaultClient.Get(Configuration["Edo:Email:Options"]).Result;
                sendGridApiKey = mailSettings[Configuration["Edo:Email:ApiKey"]];
                senderAddress = mailSettings[Configuration["Edo:Email:SenderAddress"]];
                customerInvitationTemplateId = mailSettings[Configuration["Edo:Email:CustomerInvitationTemplateId"]];
                administratorInvitationTemplateId = mailSettings[Configuration["Edo:Email:AdministratorInvitationTemplateId"]];
                externalPaymentsMailTemplateId = mailSettings[Configuration["Edo:Email:ExternalPaymentsTemplateId"]];

                if (!HostingEnvironment.IsDevelopment())
                {
                    authorityOptions = vaultClient.Get(Configuration["Authority:Options"]).Result;
                    dataProvidersOptions = vaultClient.Get(Configuration["DataProviders:Options"]).Result;
                    paymentLinksOptions = vaultClient.Get(Configuration["PaymentLinks:Options"]).Result;
                }
            }

            services.Configure<SenderOptions>(options =>
            {
                options.ApiKey = sendGridApiKey;
                options.SenderAddress = new EmailAddress(senderAddress);
            });

            var paymentLinksEndpoint = HostingEnvironment.IsDevelopment()
                ? Configuration.GetSection("PaymentLinks:Endpoint").Get<string>()
                : paymentLinksOptions["endpoint"];

            services.Configure<PaymentLinkOptions>(options =>
            {
                options.ClientSettings = new ClientSettings
                {
                    Currencies = Configuration.GetSection("PaymentLinks:Currencies")
                        .Get<List<Currencies>>(),
                    ServiceTypes = Configuration.GetSection("PaymentLinks:ServiceTypes")
                        .Get<Dictionary<ServiceTypes, string>>()
                };
                options.MailTemplateId = externalPaymentsMailTemplateId;
                options.SupportedVersions = new List<Version> {new Version(0, 2)};
                options.PaymentUrlPrefix = new Uri(paymentLinksEndpoint);
            });

            services.Configure<CustomerInvitationOptions>(options =>
                options.MailTemplateId = customerInvitationTemplateId);
            services.Configure<AdministratorInvitationOptions>(options => 
                options.MailTemplateId = administratorInvitationTemplateId);
            services.Configure<UserInvitationOptions>(options =>
                options.InvitationExpirationPeriod = TimeSpan.FromDays(7));
            

            services.AddEntityFrameworkNpgsql().AddDbContextPool<EdoContext>(options =>
            {
                var host = databaseOptions["host"];
                var port = databaseOptions["port"];
                var password = databaseOptions["password"];
                var userId = databaseOptions["userId"];

                var connectionString = Configuration.GetConnectionString("Edo");
                options.UseNpgsql(string.Format(connectionString, host, port, userId, password), builder =>
                    {
                        builder.UseNetTopologySuite();
                        builder.EnableRetryOnFailure();
                    });
                options.EnableSensitiveDataLogging(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, 16);

            var apiName = Configuration["Authority:ApiName"];
            var authorityUrl = Configuration["Authority:Endpoint"];
            if (!HostingEnvironment.IsDevelopment())
            {
                apiName = authorityOptions["apiName"];
                authorityUrl = authorityOptions["authorityUrl"];
            }

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityUrl;
                    options.ApiName = apiName;
                    options.RequireHttpsMetadata = true;
                    options.SupportedTokens = SupportedTokens.Jwt;
                });

            services.AddTransient(s => new DiscoveryClient(authorityUrl));

            services.AddHttpClient(HttpClientNames.GoogleMaps, c =>
                {
                    c.BaseAddress = new Uri(Configuration["Edo:Google:Endpoint"]);
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());
            
            services.AddHttpClient(HttpClientNames.SendGrid)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            services.AddHttpClient(HttpClientNames.Payfort)
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetDefaultRetryPolicy());

            services.Configure<GoogleOptions>(options =>
                {
                    options.ApiKey = googleOptions["apiKey"];
                })
                .Configure<FlowOptions>(options =>
                    {
                        options.CacheKeyDelimiter = "::";
                        options.CacheKeyPrefix = "HappyTravel::Edo::Api";
                    })
                .Configure<RequestLocalizationOptions>(options =>
                {
                    options.DefaultRequestCulture = new RequestCulture("en");
                    options.SupportedCultures = new[]
                    {
                        new CultureInfo("en"),
                        new CultureInfo("ar"),
                        new CultureInfo("ru")
                    };

                    options.RequestCultureProviders.Insert(0, new RouteDataRequestCultureProvider {Options = options});
                })
                .Configure<DataProviderOptions>(options =>
                {
                    var netstormingEndpoint = HostingEnvironment.IsDevelopment()
                        ? Configuration["DataProviders:NetstormingConnector"]
                        : dataProvidersOptions["netstormingConnector"];

                    options.Netstorming = netstormingEndpoint;
                })
                .Configure<PayfortOptions>(options =>
                {
                    options.AccessCode = payfortOptions["access-code"];
                    options.Identifier = payfortOptions["merchant-identifier"];
                    options.ShaRequestPhrase = payfortOptions["request-phrase"];
                    options.ShaResponsePhrase = payfortOptions["response-phrase"];
                    options.PaymentUrl = payfortUrlsOptions["payment"];
                    options.TokenizationUrl = payfortUrlsOptions["tokenization"];
                    options.ReturnUrl = payfortUrlsOptions["return"];
                    options.ResultUrl = payfortUrlsOptions["result"];
                });

            services.AddSingleton(NtsGeometryServices.Instance.CreateGeometryFactory(DefaultReferenceId));

            services.AddTransient<IDataProviderClient, DataProviderClient>();
            services.AddTransient<ICountryService, CountryService>();
            services.AddTransient<IGeoCoder, GoogleGeoCoder>();
            services.AddTransient<IGeoCoder, InteriorGeoCoder>();
            services.AddTransient<ILocationService, LocationService>();
            services.AddTransient<ICompanyService, CompanyService>();
            services.AddTransient<ICustomerService, CustomerService>();
            services.AddTransient<ICustomerRegistrationService, CustomerRegistrationService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IAccommodationService, AccommodationService>();
            services.AddScoped<ICustomerContext, HttpBasedCustomerContext>();
            services.AddHttpContextAccessor();
            services.AddSingleton<IDateTimeProvider, DefaultDateTimeProvider>();
            services.AddSingleton<IAvailabilityResultsCache, AvailabilityResultsCache>();
            services.AddTransient<IAccommodationBookingManager, AccommodationBookingManager>();
            services.AddTransient<ITagGenerator, TagGenerator>();
            
            services.AddTransient<ICustomerInvitationService, CustomerInvitationService>();
            services.AddSingleton<IMailSender, MailSender>();
            services.AddSingleton<ITokenInfoAccessor, TokenInfoAccessor>();
            services.AddTransient<IAccountBalanceAuditService, AccountBalanceAuditService>();

            services.AddTransient<IAccountManagementService, AccountManagementService>();
            services.AddScoped<IAdministratorContext, HttpBasedAdministratorContext>();
            services.AddScoped<IServiceAccountContext, HttpBasedServiceAccountContext>();

            services.AddTransient<IUserInvitationService, UserInvitationService>();
            services.AddTransient<IAdministratorInvitationService, AdministratorInvitationService>();
            services.AddTransient<IExternalAdminContext, ExternalAdminContext>();

            services.AddTransient<IAdministratorRegistrationService, AdministratorRegistrationService>();
            services.AddScoped<IManagementAuditService, ManagementAuditService>();

            services.AddScoped<IEntityLocker, EntityLocker>();
            services.AddTransient<IPaymentProcessingService, PaymentProcessingService>();

            services.AddTransient<IPayfortService, PayfortService>();
            services.AddTransient<ICreditCardService, CreditCardService>();
            services.AddTransient<IPayfortSignatureService, PayfortSignatureService>();

            services.AddTransient<IMarkupService, MarkupService>();
            services.AddTransient<IAvailabilityMarkupService, AvailabilityMarkupService>();

            services.AddSingleton<IMarkupPolicyTemplateService, MarkupPolicyTemplateService>();
            services.AddScoped<IMarkupPolicyManager, MarkupPolicyManager>();

            services.AddScoped<ICurrencyRateService, CurrencyRateService>();

            services.AddTransient<ICancellationPoliciesService, CancellationPoliciesService>();

            services.AddTransient<ISupplierOrderService, SupplierOrderService>();
            services.AddTransient<IMarkupLogger, MarkupLogger>();

            services.AddSingleton<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddTransient<ICustomerSettingsManager, CustomerSettingsManager>();

            services.AddTransient<IPaymentLinkService, PaymentLinkService>();
            services.AddTransient<IPaymentLinksProcessingService, PaymentLinksProcessingService>();

            services.AddHealthChecks()
                .AddDbContextCheck<EdoContext>();

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = false;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1.0", new Info {Title = "HappyTravel.com Edo API", Version = "v1.0" });

                var xmlCommentsFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlCommentsFilePath = Path.Combine(AppContext.BaseDirectory, xmlCommentsFileName);
                options.IncludeXmlComments(xmlCommentsFilePath);
                options.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });

                options.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>>
                {
                    {"Bearer", new string[] { }}
                });
            });
        }


        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptions<RequestLocalizationOptions> localizationOptions)
        {
            app.UseBentoExceptionHandler(env.IsProduction());

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1.0/swagger.json", "HappyTravel.com Edo API");
                options.RoutePrefix = string.Empty;
            });
            app.UseHttpContextLogging();
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyHeader());
            app.UseHealthChecks("/health");
            app.UseResponseCompression();
            
            var headersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor,
                RequireHeaderSymmetry = false,
                ForwardLimit = null
            };
            headersOptions.KnownNetworks.Clear();
            headersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(headersOptions);
            app.UseAuthentication();
            app.UseMvc();
        }


        private IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy()
        {
            var jitter = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt 
                    => TimeSpan.FromMilliseconds(Math.Pow(500, attempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 100)));
        }


        private const int DefaultReferenceId = 4326;
    }
}
