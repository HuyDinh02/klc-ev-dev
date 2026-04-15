using System.Threading.RateLimiting;
using KLC.Configuration;
using KLC.Driver;
using KLC.Driver.Endpoints;
using KLC.Driver.Middleware;
using KLC.Driver.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add Sentry error tracking
builder.WebHost.UseSentry(o =>
{
    o.Dsn = builder.Configuration["Sentry:Dsn"] ?? "";
    o.Environment = builder.Environment.EnvironmentName;
    o.TracesSampleRate = builder.Environment.IsProduction() ? 0.1 : 1.0;
    o.SendDefaultPii = false;
});

// Add ABP with Autofac
builder.Host.UseAutofac();
builder.Services.AddApplication<DriverBffModule>();

// Configure JSON to accept string enum values (e.g., "VnPay" instead of 4)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Configure services
builder.Services.AddOpenApi();

// Add CORS for mobile app — restrictive in production
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins?.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Production: no CORS (mobile apps don't need it)
            policy.SetIsOriginAllowed(_ => false);
        }
    });
});

// Add Redis caching (lazy connection — allows startup even if Redis is temporarily unavailable)
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = ConfigurationOptions.Parse(redisConnection);
    config.AbortOnConnectFail = false;
    config.ConnectRetry = 3;
    config.ConnectTimeout = 5000;
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Add HttpClient for payment gateways (MoMo API)
builder.Services.AddHttpClient();

// Initialize Firebase Admin SDK (for Phone Auth + Push Notifications)
if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
{
    var firebaseCredPath = builder.Configuration["Firebase:CredentialPath"];
    var firebaseProjectId = builder.Configuration["Firebase:ProjectId"] ?? "klc-ev-charging";

    if (!string.IsNullOrEmpty(firebaseCredPath) && System.IO.File.Exists(firebaseCredPath))
    {
        FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
        {
            Credential = Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(firebaseCredPath),
            ProjectId = firebaseProjectId
        });
    }
    else
    {
        try
        {
            FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
            {
                Credential = Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault(),
                ProjectId = firebaseProjectId
            });
        }
        catch { /* Firebase will be unavailable — auth falls back to password login */ }
    }
}

// Typed configuration (Options Pattern)
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.Section));
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection(VnPaySettings.Section));
builder.Services.Configure<ZaloPaySettings>(builder.Configuration.GetSection(ZaloPaySettings.Section));
builder.Services.Configure<MoMoSettings>(builder.Configuration.GetSection(MoMoSettings.Section));
builder.Services.Configure<WalletSettings>(builder.Configuration.GetSection(WalletSettings.Section));

// Add BFF services
builder.Services.AddScoped<IStationBffService, StationBffService>();
builder.Services.AddScoped<ISessionBffService, SessionBffService>();
builder.Services.AddScoped<IPaymentBffService, PaymentBffService>();
builder.Services.AddTransient<KLC.Payments.IPaymentGatewayService, KLC.Payments.VnPayPaymentService>();
builder.Services.AddTransient<KLC.Payments.IPaymentGatewayService, KLC.Payments.ZaloPayPaymentService>();
builder.Services.AddScoped<KLC.Payments.IPaymentCallbackValidator, KLC.Payments.PaymentCallbackValidator>();
builder.Services.AddScoped<IProfileBffService, ProfileBffService>();
builder.Services.AddScoped<IVehicleBffService, VehicleBffService>();
builder.Services.AddScoped<INotificationBffService, NotificationBffService>();
builder.Services.AddScoped<KLC.Auth.IAuthAppService, KLC.Auth.AuthAppService>();
builder.Services.AddScoped<IAuthBffService, AuthBffService>();
builder.Services.AddScoped<KLC.Payments.IWalletAppService, KLC.Payments.WalletAppService>();
builder.Services.AddScoped<KLC.Sessions.ISessionBffAppService, KLC.Sessions.SessionBffAppService>();
builder.Services.AddScoped<KLC.Payments.IPaymentProcessingAppService, KLC.Payments.PaymentProcessingAppService>();
builder.Services.AddScoped<IWalletBffService, WalletBffService>();
builder.Services.AddScoped<IFavoriteBffService, FavoriteBffService>();
builder.Services.AddScoped<KLC.Marketing.IVoucherRedemptionAppService, KLC.Marketing.VoucherRedemptionAppService>();
builder.Services.AddScoped<IVoucherBffService, VoucherBffService>();
builder.Services.AddScoped<IPromotionBffService, PromotionBffService>();
builder.Services.AddScoped<KLC.Users.IProfileAppService, KLC.Users.ProfileAppService>();
builder.Services.AddScoped<KLC.Marketing.IPromotionClaimAppService, KLC.Marketing.PromotionClaimAppService>();
builder.Services.AddScoped<IFeedbackBffService, FeedbackBffService>();

// Register services from Application layer (not auto-registered since BFF doesn't depend on KLCApplicationModule)
// SMS: configurable via Sms:Provider ("eSMS" | "SpeedSMS" | "Log")
// Default: "Log" — OTP logged to Cloud Logging (dev/testing mode)
builder.Services.AddTransient<KLC.Notifications.ISmsService, KLC.Notifications.SmsService>();
builder.Services.AddTransient<KLC.Notifications.IPushNotificationService, KLC.Notifications.FirebasePushNotificationService>();
// File storage provider — configurable per environment (GCS, S3, or local)
builder.Services.Configure<KLC.Configuration.FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));
var fileProvider = builder.Configuration["FileStorage:Provider"]?.ToLowerInvariant() ?? "gcs";
switch (fileProvider)
{
    case "s3":
        builder.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.S3FileUploadService>();
        break;
    case "local":
        builder.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.LocalFileUploadService>();
        break;
    default:
        builder.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.GcsFileUploadService>();
        break;
}
builder.Services.AddTransient<KLC.Auditing.IAuditEventLogger, KLC.Auditing.AuditEventLogger>();

// Add SignalR for real-time updates
builder.Services.AddSignalR();
builder.Services.AddScoped<IDriverHubNotifier, DriverHubNotifier>();

// Add authentication — validate BFF-issued JWTs with symmetric key
var jwtKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "Jwt:SecretKey is not configured. Set it in appsettings.json or environment variable Jwt__SecretKey.");
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "KLC.Driver.BFF",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "KLC.Driver.App",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Auth endpoints: 10 requests per minute per IP (brute force protection)
    // Use X-Forwarded-For on Cloud Run (RemoteIpAddress is the GFE proxy)
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    // General API: 300 requests per minute per user/IP
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!,
        tags: ["db"], failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded)
    .AddRedis(redisConnection,
        tags: ["cache"], failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);

var app = builder.Build();

// Validate production configuration
if (app.Environment.IsProduction())
{
    ValidateProductionConfiguration(app.Configuration, app.Logger);
}

// Initialize ABP
await app.InitializeApplicationAsync();

// Configure middleware — API docs via config flag (safe to enable temporarily in non-prod)
var enableApiDocs = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("EnableApiDocs");
if (enableApiDocs)
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "KLC Driver BFF API";
        options.Theme = ScalarTheme.BluePlanet;
        options.DefaultHttpClient = new(ScalarTarget.JavaScript, ScalarClient.Fetch);
    });
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<UnitOfWorkMiddleware>();
app.UseSentryTracing();
app.UseCors("MobileApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<SuspendedUserMiddleware>();

// Liveness probe — always returns 200 if the process is running (no dependency checks)
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// Readiness probe — checks DB + Redis connectivity
app.MapHealthChecks("/health/ready");

// Map API endpoints
app.MapConfigEndpoints();
app.MapAuthEndpoints();
app.MapStationEndpoints();
app.MapSessionEndpoints();
app.MapPaymentEndpoints();
app.MapProfileEndpoints();
app.MapVehicleEndpoints();
app.MapNotificationEndpoints();
app.MapWalletEndpoints();
app.MapFavoriteEndpoints();
app.MapVoucherEndpoints();
app.MapPromotionEndpoints();
app.MapFeedbackEndpoints();

// Map SignalR hub
app.MapHub<DriverHub>("/hubs/driver");

await app.RunAsync();

static void ValidateProductionConfiguration(IConfiguration config, ILogger logger)
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

    // Critical: Redis connection must not be the dev default
    var redisConn = config.GetConnectionString("Redis") ?? "";
    if (string.IsNullOrWhiteSpace(redisConn) || redisConn == "localhost:6379")
    {
        logger.LogCritical(
            "PRODUCTION CONFIG ERROR: ConnectionStrings:Redis is using the development default 'localhost:6379'. " +
            "Configure a production Redis connection string via environment variable or Secret Manager.");
        hasErrors = true;
    }

    // Critical: Jwt:SecretKey must be configured and not the dev default
    var jwtSecret = config["Jwt:SecretKey"] ?? "";
    if (string.IsNullOrWhiteSpace(jwtSecret) ||
        jwtSecret == "KLC_DEFAULT_JWT_SECRET_KEY_FOR_DEVELOPMENT_ONLY_2026")
    {
        logger.LogCritical(
            "PRODUCTION CONFIG ERROR: Jwt:SecretKey is missing or using the development default. " +
            "Configure a strong secret key via environment variable Jwt__SecretKey or Secret Manager.");
        hasErrors = true;
    }

    // Critical: VnPay credentials must be configured (payments will silently fail without them)
    var vnpaySecret = config["Payment:VnPay:HashSecret"] ?? "";
    if (string.IsNullOrWhiteSpace(vnpaySecret))
    {
        logger.LogCritical(
            "PRODUCTION CONFIG ERROR: Payment:VnPay:HashSecret is not configured. " +
            "VnPay top-ups will fail until this is set via Secret Manager.");
        hasErrors = true;
    }

    var vnpayTmnCode = config["Payment:VnPay:TmnCode"] ?? "";
    if (string.IsNullOrWhiteSpace(vnpayTmnCode))
    {
        logger.LogCritical(
            "PRODUCTION CONFIG ERROR: Payment:VnPay:TmnCode is not configured. " +
            "VnPay top-ups will fail until this is set via Secret Manager.");
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
