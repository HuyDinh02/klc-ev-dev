using System.Threading.RateLimiting;
using KLC.Driver;
using KLC.Driver.Endpoints;
using KLC.Driver.Services;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add ABP with Autofac
builder.Host.UseAutofac();
builder.Services.AddApplication<DriverBffModule>();

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

// Add BFF services
builder.Services.AddScoped<IStationBffService, StationBffService>();
builder.Services.AddScoped<ISessionBffService, SessionBffService>();
builder.Services.AddScoped<IPaymentBffService, PaymentBffService>();
builder.Services.AddScoped<IProfileBffService, ProfileBffService>();
builder.Services.AddScoped<IVehicleBffService, VehicleBffService>();
builder.Services.AddScoped<INotificationBffService, NotificationBffService>();
builder.Services.AddScoped<IAuthBffService, AuthBffService>();
builder.Services.AddScoped<IWalletBffService, WalletBffService>();
builder.Services.AddScoped<IFavoriteBffService, FavoriteBffService>();
builder.Services.AddScoped<IVoucherBffService, VoucherBffService>();
builder.Services.AddScoped<IFeedbackBffService, FeedbackBffService>();

// Register services from Application layer (not auto-registered since BFF doesn't depend on KLCApplicationModule)
builder.Services.AddTransient<KLC.Notifications.ISmsService, KLC.Notifications.LogOnlySmsService>();
builder.Services.AddTransient<KLC.Files.IFileUploadService, KLC.Files.GcsFileUploadService>();

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

    // Auth endpoints: 10 requests per minute per IP
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    // General API: 60 requests per minute per user/IP
    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
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

app.UseCors("MobileApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Map health check
app.MapHealthChecks("/health");

// Map API endpoints
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
app.MapFeedbackEndpoints();

// Map SignalR hub
app.MapHub<DriverHub>("/hubs/driver");

await app.RunAsync();
