using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KLC.EntityFrameworkCore;
using KLC.Hubs;
using KLC.MultiTenancy;
using KLC.Ocpp;
using KLC.Ocpp.Vendors;
using Volo.Abp.AspNetCore.ExceptionHandling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.OpenApi;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Swashbuckle;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;

namespace KLC;

[DependsOn(
    typeof(KLCHttpApiModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(KLCApplicationModule),
    typeof(KLCEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpSwashbuckleModule)
)]
public class KLCHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("KLC");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        // Configure OpenIddict server signing/encryption keys
        PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
        {
            if (hostingEnvironment.IsDevelopment())
            {
                // Development: use ephemeral (in-memory) keys
                serverBuilder.AddEphemeralEncryptionKey()
                             .AddEphemeralSigningKey();
            }
            else
            {
                // Production: load persistent X.509 certificates from config or files
                // Certificates are stored as base64-encoded PFX in environment variables
                // (injected from GCP Secret Manager via Cloud Run)
                var signingCertBase64 = Environment.GetEnvironmentVariable("OPENIDDICT_SIGNING_CERT");
                var signingCertPassword = Environment.GetEnvironmentVariable("OPENIDDICT_SIGNING_PASSWORD");
                var encryptionCertBase64 = Environment.GetEnvironmentVariable("OPENIDDICT_ENCRYPTION_CERT");
                var encryptionCertPassword = Environment.GetEnvironmentVariable("OPENIDDICT_ENCRYPTION_PASSWORD");

                if (!string.IsNullOrEmpty(signingCertBase64) && !string.IsNullOrEmpty(encryptionCertBase64))
                {
                    var signingCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        Convert.FromBase64String(signingCertBase64),
                        signingCertPassword,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);

                    var encryptionCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        Convert.FromBase64String(encryptionCertBase64),
                        encryptionCertPassword,
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.MachineKeySet |
                        System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);

                    serverBuilder.AddSigningCertificate(signingCert)
                                 .AddEncryptionCertificate(encryptionCert);
                }
                else
                {
                    // Fallback to ephemeral if certs not configured yet
                    serverBuilder.AddEphemeralEncryptionKey()
                                 .AddEphemeralSigningKey();
                }
            }

            serverBuilder.DisableAccessTokenEncryption();
        });
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        // Disable ABP libs check — this is an API-only host, no frontend assets needed
        Configure<Volo.Abp.AspNetCore.Mvc.Libs.AbpMvcLibsOptions>(options =>
        {
            options.CheckLibs = false;
        });

        ConfigureAuthentication(context);
        ConfigureBundles();
        ConfigureUrls(configuration);
        ConfigureConventionalControllers();
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSwaggerServices(context, configuration);
        ConfigureOcppServices(context);
        ConfigureSignalR(context);
        ConfigureExceptionHttpStatusCodes();
        ConfigureHealthChecks(context, configuration);
        ConfigureRateLimiting(context);
        ConfigureHttpClients(context);
    }

    private static void ConfigureRateLimiting(ServiceConfigurationContext context)
    {
        context.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            // Global: 100 requests per minute per IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
    }

    private void ConfigureExceptionHttpStatusCodes()
    {
        Configure<AbpExceptionHttpStatusCodeOptions>(options =>
        {
            // 404 Not Found
            options.Map(KLCDomainErrorCodes.Station.NotFound, HttpStatusCode.NotFound);
            options.Map(KLCDomainErrorCodes.Connector.StationNotFound, HttpStatusCode.NotFound);
            options.Map(KLCDomainErrorCodes.Session.ConnectorNotFound, HttpStatusCode.NotFound);
            options.Map(KLCDomainErrorCodes.Payment.NotFound, HttpStatusCode.NotFound);
            options.Map(KLCDomainErrorCodes.InvoiceNotFound, HttpStatusCode.NotFound);
            options.Map(KLCDomainErrorCodes.Notification.NotOwned, HttpStatusCode.NotFound);

            // 409 Conflict (duplicate / already exists)
            options.Map(KLCDomainErrorCodes.Station.DuplicateCode, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Connector.DuplicateNumber, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Session.AlreadyActive, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Payment.AlreadyCompleted, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.EInvoiceAlreadyExists, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.EInvoiceAlreadyCancelled, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.UserNameAlreadyExists, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.EmailAlreadyExists, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.RoleNameAlreadyExists, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.CannotDeleteRoleWithUsers, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Profile.EmailAlreadyUsed, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Profile.PhoneAlreadyUsed, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Profile.HasActiveSession, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.StationGroup.StationAlreadyAssigned, HttpStatusCode.Conflict);

            // 422 Unprocessable Entity (invalid state transition / business rule)
            options.Map(KLCDomainErrorCodes.Fault.InvalidStatusTransition, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Session.ConnectorNotAvailable, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Session.InvalidStatus, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Session.InvalidStateTransition, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Session.NoDefaultVehicle, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.EInvoiceCannotRetry, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Payment.InvalidRefund, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Payment.CannotCancel, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Payment.SessionNotCompleted, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Wallet.InsufficientBalance, HttpStatusCode.UnprocessableEntity);

            // 400 Bad Request (input/validation)
            options.Map(KLCDomainErrorCodes.Station.InvalidLatitude, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Station.InvalidLongitude, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Connector.MaxPowerInvalid, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Tariff.InvalidBaseRate, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Tariff.InvalidTaxRate, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Tariff.InvalidEffectivePeriod, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Fault.InvalidPriority, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Alert.InvalidPriority, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.UserCreationFailed, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.PasswordResetFailed, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Profile.PasswordChangeFailed, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.CannotUpdateStaticRole, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.CannotDeleteStaticRole, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.CannotDeleteCurrentUser, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.CannotLockCurrentUser, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.StationGroup.StationNotInGroup, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Alert.InvalidAcknowledge, HttpStatusCode.BadRequest);

            // 403 Forbidden (ownership)
            options.Map(KLCDomainErrorCodes.Payment.SessionNotOwned, HttpStatusCode.Forbidden);
            options.Map(KLCDomainErrorCodes.Payment.NotOwned, HttpStatusCode.Forbidden);
            options.Map(KLCDomainErrorCodes.Payment.MethodNotOwned, HttpStatusCode.Forbidden);
            options.Map(KLCDomainErrorCodes.Vehicle.NotOwned, HttpStatusCode.Forbidden);
            options.Map(KLCDomainErrorCodes.Session.NotOwned, HttpStatusCode.Forbidden);
            options.Map(KLCDomainErrorCodes.Payment.InvalidSignature, HttpStatusCode.Forbidden);
        });
    }

    private void ConfigureOcppServices(ServiceConfigurationContext context)
    {
        // Register OCPP services
        context.Services.AddSingleton<OcppConnectionManager>();
        context.Services.AddSingleton<OcppMessageParserFactory>();
        context.Services.AddScoped<OcppMessageHandler>();
        context.Services.AddHostedService<HeartbeatMonitorService>();
        context.Services.AddScoped<IOcppRemoteCommandService, OcppRemoteCommandService>();

        // Vendor profiles
        context.Services.AddSingleton<IVendorProfile, GenericProfile>();
        context.Services.AddSingleton<IVendorProfile, ChargecoreGlobalProfile>();
        context.Services.AddSingleton<IVendorProfile, JuhangProfile>();
        context.Services.AddSingleton<VendorProfileFactory>();
    }

    private void ConfigureSignalR(ServiceConfigurationContext context)
    {
        // Add SignalR for real-time monitoring
        context.Services.AddSignalR();
        context.Services.AddScoped<IMonitoringNotifier, MonitoringNotifier>();
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());

            options.Applications["Angular"].RootUrl = configuration["App:ClientUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
        });
    }

    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<KLCDomainSharedModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}KLC.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<KLCDomainModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}KLC.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<KLCApplicationContractsModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}KLC.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<KLCApplicationModule>(
                    Path.Combine(hostingEnvironment.ContentRootPath,
                        $"..{Path.DirectorySeparatorChar}KLC.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(KLCApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwaggerServices(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOAuth(
            configuration["AuthServer:Authority"]!,
            new Dictionary<string, string>
            {
                    {"KLC", "KLC API"}
            },
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "KLC API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(configuration["App:CorsOrigins"]?
                        .Split(",", StringSplitOptions.RemoveEmptyEntries)
                        .Select(o => o.RemovePostFix("/"))
                        .ToArray() ?? Array.Empty<string>())
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private static void ConfigureHttpClients(ServiceConfigurationContext context)
    {
        // HttpClient for payment gateways (MoMo API)
        context.Services.AddHttpClient();
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Default")!,
                tags: ["db"],
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        // Trust forwarded headers from Cloud Run load balancer (X-Forwarded-For, X-Forwarded-Proto)
        // This ensures OpenIddict sees HTTPS scheme even though Cloud Run terminates TLS
        var forwardedHeadersOptions = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                             | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        };
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSentryTracing();
        app.UseAbpRequestLocalization();

        // Don't use ABP error page — it redirects API 403/500 to /Error HTML page
        // which breaks CORS for SPA clients. ABP exception filter returns proper JSON.

        app.UseCorrelationId();
        app.MapAbpStaticAssets();

        // Enable WebSockets for OCPP
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120)
        });

        // OCPP WebSocket middleware (before routing)
        app.UseMiddleware<OcppWebSocketMiddleware>();

        app.UseRouting();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }
        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseAbpSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "KLC API");

                var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
                c.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
                c.OAuthScopes("KLC");
            });
        }

        app.UseAuditing();
        app.UseAbpSerilogEnrichers();
        app.UseConfiguredEndpoints(endpoints =>
        {
            // Map SignalR hub for real-time monitoring
            endpoints.MapHub<MonitoringHub>("/hubs/monitoring");
            endpoints.MapHealthChecks("/health");
        });
    }
}
