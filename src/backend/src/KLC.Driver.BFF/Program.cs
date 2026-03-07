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

// Add CORS for mobile app
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Redis caching
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

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
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "KLC_DEFAULT_JWT_SECRET_KEY_FOR_DEVELOPMENT_ONLY_2026";
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
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

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!)
    .AddRedis(redisConnection);

var app = builder.Build();

// Initialize ABP
await app.InitializeApplicationAsync();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("MobileApp");
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
