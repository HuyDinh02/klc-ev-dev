using Serilog;
using Volo.Abp.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host
    .AddAppSettingsSecretsJson()
    .UseAutofac()
    .UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/klc-csms-.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
    });

// Add ABP application
await builder.AddApplicationAsync<KLC.KlcHttpApiHostModule>();

var app = builder.Build();

// Initialize ABP application
await app.InitializeApplicationAsync();

// WebSocket support
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// OCPP WebSocket middleware (before routing)
app.UseOcppWebSocket("/ocpp");

// Standard ASP.NET Core middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<KLC.Hubs.ChargingHub>("/signalr/charging");

// Health check endpoint
app.MapHealthChecks("/health");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
