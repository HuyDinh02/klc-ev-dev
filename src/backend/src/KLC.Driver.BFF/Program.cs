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

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false; // Dev only
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
app.MapStationEndpoints();
app.MapSessionEndpoints();
app.MapPaymentEndpoints();
app.MapProfileEndpoints();
app.MapVehicleEndpoints();
app.MapNotificationEndpoints();

// Map SignalR hub
app.MapHub<DriverHub>("/hubs/driver");

await app.RunAsync();
