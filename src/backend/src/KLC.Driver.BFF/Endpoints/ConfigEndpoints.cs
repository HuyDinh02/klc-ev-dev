using KLC.Configuration;
using Microsoft.Extensions.Options;

namespace KLC.Driver.Endpoints;

public static class ConfigEndpoints
{
    public static void MapConfigEndpoints(this IEndpointRouteBuilder app)
    {
        // GET /api/v1/config — Remote config for mobile app
        // Public endpoint (no auth) so app can fetch on startup before login
        // Cache: 5 min client-side, 1 min CDN — reduces requests from 500+ users
        app.MapGet("/api/v1/config", (
            HttpContext httpContext,
            IOptions<WalletSettings> walletSettings,
            IConfiguration configuration) =>
        {
            // HTTP cache: mobile caches 5 min, CDN/proxy caches 1 min
            httpContext.Response.Headers.CacheControl = "public, max-age=300, s-maxage=60";

            var wallet = walletSettings.Value;

            return Results.Ok(new AppConfigResponse
            {
                Wallet = new WalletConfig
                {
                    MinTopUpAmount = wallet.MinTopUpAmount,
                    MaxTopUpAmount = wallet.MaxTopUpAmount,
                    MinBalanceToStart = wallet.MinBalanceToStart,
                    LowBalanceThreshold = wallet.LowBalanceThreshold,
                    TopUpPresets = new[] { 50_000m, 100_000m, 200_000m, 500_000m },
                },
                Charging = new ChargingConfig
                {
                    MinBalanceToStart = wallet.MinBalanceToStart,
                    AutoStopThreshold = wallet.LowBalanceThreshold,
                },
                Map = new MapConfig
                {
                    DefaultRadiusKm = configuration.GetValue("Map:DefaultRadiusKm", 30),
                    MaxRadiusKm = configuration.GetValue("Map:MaxRadiusKm", 100),
                    DefaultLatitude = configuration.GetValue("Map:DefaultLatitude", 10.7769),
                    DefaultLongitude = configuration.GetValue("Map:DefaultLongitude", 106.7009),
                    DefaultZoom = configuration.GetValue("Map:DefaultZoom", 13),
                },
                App = new AppInfo
                {
                    MinVersion = configuration["App:MinVersion"] ?? "1.0.0",
                    LatestVersion = configuration["App:LatestVersion"] ?? "1.0.0",
                    ForceUpdate = configuration.GetValue("App:ForceUpdate", false),
                    MaintenanceMode = configuration.GetValue("App:MaintenanceMode", false),
                    MaintenanceMessage = configuration["App:MaintenanceMessage"],
                },
                Contact = new ContactInfo
                {
                    Hotline = configuration["App:Hotline"] ?? "1900 2010",
                    Email = configuration["App:Email"] ?? "support@klc.vn",
                    Website = configuration["App:Website"] ?? "https://klc.vn",
                }
            });
        })
        .WithName("GetAppConfig")
        .WithSummary("Remote config for mobile app — thresholds, presets, app version, maintenance mode")
        .Produces<AppConfigResponse>(200)
        .AllowAnonymous();
    }
}

public record AppConfigResponse
{
    public WalletConfig Wallet { get; init; } = new();
    public ChargingConfig Charging { get; init; } = new();
    public MapConfig Map { get; init; } = new();
    public AppInfo App { get; init; } = new();
    public ContactInfo Contact { get; init; } = new();
}

public record WalletConfig
{
    /// <summary>Minimum top-up amount (VND)</summary>
    public decimal MinTopUpAmount { get; init; }
    /// <summary>Maximum top-up amount per transaction (VND)</summary>
    public decimal MaxTopUpAmount { get; init; }
    /// <summary>Minimum wallet balance to start charging (VND)</summary>
    public decimal MinBalanceToStart { get; init; }
    /// <summary>Auto-stop charging when balance drops below (VND)</summary>
    public decimal LowBalanceThreshold { get; init; }
    /// <summary>Preset top-up amounts for quick selection buttons</summary>
    public decimal[] TopUpPresets { get; init; } = [];
}

public record ChargingConfig
{
    /// <summary>Minimum wallet balance to start charging (VND)</summary>
    public decimal MinBalanceToStart { get; init; }
    /// <summary>System auto-stops charging when remaining balance below this (VND)</summary>
    public decimal AutoStopThreshold { get; init; }
}

public record AppInfo
{
    /// <summary>Minimum app version required (force update if below)</summary>
    public string MinVersion { get; init; } = "1.0.0";
    /// <summary>Latest available version</summary>
    public string LatestVersion { get; init; } = "1.0.0";
    /// <summary>Force users to update if below MinVersion</summary>
    public bool ForceUpdate { get; init; }
    /// <summary>Show maintenance screen if true</summary>
    public bool MaintenanceMode { get; init; }
    /// <summary>Maintenance message to display</summary>
    public string? MaintenanceMessage { get; init; }
}

public record MapConfig
{
    /// <summary>Default search radius for nearby stations (km)</summary>
    public int DefaultRadiusKm { get; init; } = 30;
    /// <summary>Maximum allowed search radius (km)</summary>
    public int MaxRadiusKm { get; init; } = 100;
    /// <summary>Default map center latitude (Ho Chi Minh City)</summary>
    public double DefaultLatitude { get; init; } = 10.7769;
    /// <summary>Default map center longitude</summary>
    public double DefaultLongitude { get; init; } = 106.7009;
    /// <summary>Default map zoom level</summary>
    public int DefaultZoom { get; init; } = 13;
}

public record ContactInfo
{
    public string Hotline { get; init; } = "";
    public string Email { get; init; } = "";
    public string Website { get; init; } = "";
}
