using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using KLC.Configuration;
using KLC.Payments;
using KLC.EntityFrameworkCore;
using KLC.Hubs;
using KLC.MultiTenancy;
using KLC.Ocpp;
using KLC.Ocpp.Handlers;
using KLC.Ocpp.Vendors;
using KLC.Operators;
using KLC.Services;
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

        // Typed configuration (Options Pattern)
        context.Services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.Section));
        context.Services.Configure<VnPaySettings>(configuration.GetSection(VnPaySettings.Section));
        context.Services.Configure<ZaloPaySettings>(configuration.GetSection(ZaloPaySettings.Section));
        context.Services.Configure<MoMoSettings>(configuration.GetSection(MoMoSettings.Section));
        context.Services.Configure<WalletSettings>(configuration.GetSection(WalletSettings.Section));

        // Explicit registration for IEnumerable<IPaymentGatewayService> injection
        // ABP's ITransientDependency uses TryAdd which only registers the first implementation.
        context.Services.AddTransient<IPaymentGatewayService, VnPayPaymentService>();
        context.Services.AddTransient<IPaymentGatewayService, MoMoPaymentService>();
        context.Services.AddTransient<IPaymentGatewayService, ZaloPayPaymentService>();

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
        ConfigureAuditing();
    }

    private void ConfigureAuditing()
    {
        Configure<Volo.Abp.Auditing.AbpAuditingOptions>(options =>
        {
            options.IsEnabledForGetRequests = true;
        });

        // Exclude noisy URLs from audit logging
        Configure<Volo.Abp.AspNetCore.Auditing.AbpAspNetCoreAuditingOptions>(options =>
        {
            options.IgnoredUrls.AddIfNotContains("/hubs/");
            options.IgnoredUrls.AddIfNotContains("/connect/token");
            options.IgnoredUrls.AddIfNotContains("/health");
            options.IgnoredUrls.AddIfNotContains("/swagger");
            options.IgnoredUrls.AddIfNotContains("/api/abp/");
        });
    }

    private static void ConfigureRateLimiting(ServiceConfigurationContext context)
    {
        context.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            // Global: 600 requests per minute per IP
            // On Cloud Run, RemoteIpAddress is the GFE proxy — use X-Forwarded-For for real client IP
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                        ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 600,
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
            options.Map(KLCDomainErrorCodes.Station.CannotEnableDecommissioned, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.EInvoiceCannotRetry, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Payment.InvalidRefund, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Payment.CannotCancel, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Payment.SessionNotCompleted, HttpStatusCode.UnprocessableEntity);
            options.Map(KLCDomainErrorCodes.Wallet.InsufficientBalance, HttpStatusCode.UnprocessableEntity);

            // 503 Service Unavailable (downstream charger command dispatch failed)
            options.Map(KLCDomainErrorCodes.Session.StartCommandFailed, HttpStatusCode.ServiceUnavailable);
            options.Map(KLCDomainErrorCodes.Session.StopCommandFailed, HttpStatusCode.ServiceUnavailable);

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
            options.Map(KLCDomainErrorCodes.Operators.NoStationAccess, HttpStatusCode.Forbidden);

            // Operator errors
            options.Map(KLCDomainErrorCodes.Operators.NotFound, HttpStatusCode.NotFound);
            options.Map(KLCDomainErrorCodes.Operators.DuplicateName, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Operators.StationAlreadyAssigned, HttpStatusCode.Conflict);
            options.Map(KLCDomainErrorCodes.Operators.StationNotAssigned, HttpStatusCode.BadRequest);
            options.Map(KLCDomainErrorCodes.Operators.InvalidApiKey, HttpStatusCode.Unauthorized);
            options.Map(KLCDomainErrorCodes.Operators.NotActive, HttpStatusCode.Forbidden);
            options.Map(KLCDomainErrorCodes.Operators.RateLimitExceeded, (HttpStatusCode)429);
        });
    }

    private void ConfigureOcppServices(ServiceConfigurationContext context)
    {
        // Register OCPP services
        context.Services.AddSingleton<OcppConnectionManager>();
        context.Services.AddSingleton<OcppMessageParserFactory>();

        // Auto-discover all IOcppActionHandler implementations (Strategy Pattern)
        var handlerTypes = typeof(IOcppActionHandler).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IOcppActionHandler).IsAssignableFrom(t));
        foreach (var type in handlerTypes)
            context.Services.AddScoped(typeof(IOcppActionHandler), type);

        context.Services.AddScoped<OcppMessageHandler>();
        context.Services.AddHostedService<HeartbeatMonitorService>();
        context.Services.AddHostedService<OrphanedSessionCleanupService>();
        context.Services.AddHostedService<FleetResetBackgroundService>();
        context.Services.AddHostedService<WalletBalanceMonitorService>();
        context.Services.AddHostedService<PaymentReconciliationService>();
        context.Services.AddSingleton<PowerBalancingService>();
        context.Services.AddHostedService<PowerBalancingService>(sp => sp.GetRequiredService<PowerBalancingService>());
        context.Services.AddSingleton<OcppRedisCommandBridge>();
        context.Services.AddHostedService<OcppRedisCommandBridge>(sp => sp.GetRequiredService<OcppRedisCommandBridge>());
        context.Services.AddScoped<IOcppRemoteCommandService, OcppRemoteCommandService>();
        context.Services.AddScoped<OcppPostBootConfigService>();

        // Auto-discover all IVendorProfile implementations (add new vendor = add 1 class file)
        var vendorProfileTypes = typeof(IVendorProfile).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IVendorProfile).IsAssignableFrom(t));
        foreach (var profileType in vendorProfileTypes)
            context.Services.AddSingleton(typeof(IVendorProfile), profileType);
        context.Services.AddSingleton<VendorProfileFactory>();

        // File storage provider — configurable per environment (GCS, S3, or local)
        var fileStorageProvider = context.Services.GetConfiguration()["FileStorage:Provider"]?.ToLowerInvariant() ?? "gcs";
        context.Services.Configure<KLC.Configuration.FileStorageSettings>(context.Services.GetConfiguration().GetSection("FileStorage"));
        switch (fileStorageProvider)
        {
            case "s3":
                context.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.S3FileUploadService>();
                break;
            case "local":
                context.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.LocalFileUploadService>();
                break;
            default: // "gcs"
                context.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.GcsFileUploadService>();
                break;
        }
    }

    private void ConfigureSignalR(ServiceConfigurationContext context)
    {
        // Add SignalR for real-time monitoring with Redis backplane
        // Redis backplane allows SignalR messages to be shared across multiple
        // Cloud Run instances (Admin API + OCPP Gateway both publish/subscribe)
        var redisConnection = context.Services.GetConfiguration()["ConnectionStrings:Redis"];
        if (!string.IsNullOrEmpty(redisConnection))
        {
            context.Services.AddSignalR().AddStackExchangeRedis(redisConnection, options =>
            {
                options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("klc-signalr");
            });
        }
        else
        {
            context.Services.AddSignalR();
        }
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
                ReplaceEmbeddedFileSetIfPresent<KLCDomainSharedModule>(options, hostingEnvironment, "KLC.Domain.Shared");
                ReplaceEmbeddedFileSetIfPresent<KLCDomainModule>(options, hostingEnvironment, "KLC.Domain");
                ReplaceEmbeddedFileSetIfPresent<KLCApplicationContractsModule>(options, hostingEnvironment, "KLC.Application.Contracts");
                ReplaceEmbeddedFileSetIfPresent<KLCApplicationModule>(options, hostingEnvironment, "KLC.Application");
            });
        }
    }

    private static void ReplaceEmbeddedFileSetIfPresent<TModule>(
        AbpVirtualFileSystemOptions options,
        IWebHostEnvironment hostingEnvironment,
        string projectFolder)
    {
        var physicalPath = Path.Combine(
            hostingEnvironment.ContentRootPath,
            $"..{Path.DirectorySeparatorChar}{projectFolder}");

        if (Directory.Exists(physicalPath))
        {
            options.FileSets.ReplaceEmbeddedByPhysical<TModule>(physicalPath);
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

        // Named HttpClient for operator webhook delivery (10s timeout)
        context.Services.AddHttpClient("OperatorWebhook", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        // Operator webhook delivery service
        context.Services.AddScoped<IOperatorWebhookService, OperatorWebhookService>();
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

        if (env.IsProduction())
        {
            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = context.ServiceProvider.GetRequiredService<ILogger<KLCHttpApiHostModule>>();
            ValidateProductionConfiguration(configuration, logger);
        }

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

        app.UseAbpRequestLocalization();

        // Don't use ABP error page — it redirects API 403/500 to /Error HTML page
        // which breaks CORS for SPA clients. ABP exception filter returns proper JSON.

        app.UseCorrelationId();

        // Security response headers
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["X-XSS-Protection"] = "0";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            if (!context.Request.Path.StartsWithSegments("/ocpp"))
            {
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            }
            await next();
        });

        app.MapAbpStaticAssets();

        // Enable WebSockets for OCPP
        app.UseWebSockets(new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromSeconds(120)
        });

        // OCPP WebSocket middleware (before routing)
        app.UseMiddleware<OcppWebSocketMiddleware>();

        // HSTS header (Cloud Run handles HTTPS but HSTS prevents downgrade attacks)
        app.UseHsts();

        app.UseRouting();
        app.UseCors();
        app.UseRateLimiter();

        // Operator API key middleware — authenticate B2B requests before standard auth
        app.UseMiddleware<Middleware.OperatorApiKeyMiddleware>();

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

            // Liveness probe — always returns 200 if the process is running (no dependency checks)
            endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
                .ExcludeFromDescription();

            // Readiness probe — checks DB connectivity
            endpoints.MapHealthChecks("/health/ready");
        });
    }

    private static void ValidateProductionConfiguration(IConfiguration config, ILogger logger)
    {
        logger.LogInformation("Validating production configuration...");

        var hasErrors = false;

        // Critical: Connection string must not be the dev default
        var connectionString = config.GetConnectionString("Default") ?? "";
        if (string.IsNullOrWhiteSpace(connectionString) ||
            connectionString.Contains("Host=localhost") ||
            connectionString.Contains("Port=5433;Database=KLC;Username=postgres;Password=postgres"))
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: ConnectionStrings:Default is using the development default. " +
                "Configure a production database connection string via environment variable or Secret Manager.");
            hasErrors = true;
        }

        // Critical: Jwt:SecretKey must be configured
        var jwtSecret = config["Jwt:SecretKey"] ?? "";
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: Jwt:SecretKey is not configured. " +
                "Set a strong secret key via environment variable Jwt__SecretKey or Secret Manager.");
            hasErrors = true;
        }

        // Critical: StringEncryption passphrase must not be the dev default
        var passPhrase = config["StringEncryption:DefaultPassPhrase"] ?? "";
        if (string.IsNullOrWhiteSpace(passPhrase) || passPhrase == "IMGWbiZSGFfhNl7G")
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: StringEncryption:DefaultPassPhrase is using the development default 'IMGWbiZSGFfhNl7G'. " +
                "Configure a unique passphrase for production via environment variable or Secret Manager.");
            hasErrors = true;
        }

        // Critical: OCPP test ID tags must be disabled
        var allowTestIdTags = config.GetValue<bool>("Ocpp:AllowTestIdTags");
        if (allowTestIdTags)
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: Ocpp:AllowTestIdTags is true. " +
                "This must be false in production to prevent unauthorized charging sessions.");
            hasErrors = true;
        }

        // Critical: HTTPS metadata must be required
        var requireHttps = config.GetValue<bool>("AuthServer:RequireHttpsMetadata");
        if (!requireHttps)
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: AuthServer:RequireHttpsMetadata is false. " +
                "This must be true in production to enforce secure token validation.");
            hasErrors = true;
        }

        // Optional: Payment gateway secrets
        var momoSecret = config["Payment:MoMo:SecretKey"] ?? "";
        if (string.IsNullOrWhiteSpace(momoSecret))
        {
            logger.LogWarning(
                "PRODUCTION CONFIG WARNING: Payment:MoMo:SecretKey is not configured. " +
                "MoMo payments will not work until this is set.");
        }

        var vnpaySecret = config["Payment:VnPay:HashSecret"] ?? "";
        if (string.IsNullOrWhiteSpace(vnpaySecret))
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: Payment:VnPay:HashSecret is not configured. " +
                "VnPay IPN validation will fail until this is set via Secret Manager.");
            hasErrors = true;
        }

        var vnpayTmnCode = config["Payment:VnPay:TmnCode"] ?? "";
        if (string.IsNullOrWhiteSpace(vnpayTmnCode))
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: Payment:VnPay:TmnCode is not configured. " +
                "VnPay IPN validation will fail until this is set via Secret Manager.");
            hasErrors = true;
        }

        // SEC-4: AllowUnregisteredIdTags must be false in production to prevent anonymous charging
        var allowUnregistered = config.GetValue<bool>("Ocpp:AllowUnregisteredIdTags", true);
        if (allowUnregistered)
        {
            logger.LogCritical(
                "PRODUCTION CONFIG ERROR: Ocpp:AllowUnregisteredIdTags is true (or not set — default is true). " +
                "Set Ocpp:AllowUnregisteredIdTags=false to require registered RFID tags in production.");
            hasErrors = true;
        }

        // Optional: Sentry DSN
        var sentryDsn = config["Sentry:Dsn"] ?? "";
        if (string.IsNullOrWhiteSpace(sentryDsn))
        {
            logger.LogWarning(
                "PRODUCTION CONFIG WARNING: Sentry:Dsn is not configured. " +
                "Error tracking will be disabled.");
        }

        if (hasErrors)
        {
            logger.LogCritical(
                "PRODUCTION CONFIG VALIDATION FAILED: One or more critical configuration issues detected. " +
                "The application will start but may not function correctly. Review the errors above.");
        }
        else
        {
            logger.LogInformation("Production configuration validation passed.");
        }
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        // Close all OCPP WebSocket connections on shutdown so chargers reconnect to new instance
        var connectionManager = context.ServiceProvider.GetRequiredService<OcppConnectionManager>();
        connectionManager.CloseAllConnectionsAsync().GetAwaiter().GetResult();
    }
}
