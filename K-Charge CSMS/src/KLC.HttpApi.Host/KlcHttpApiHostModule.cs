using KLC.Hubs;
using KLC.Ocpp;
using KLC.Ocpp.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace KLC;

[DependsOn(
    typeof(AbpAspNetCoreSignalRModule)
    // typeof(KlcApplicationModule),
    // typeof(KlcEntityFrameworkCoreModule),
)]
public class KlcHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // ── OCPP WebSocket Infrastructure (singletons) ──
        services.AddSingleton<OcppConnectionManager>();
        services.AddSingleton<IOcppMessageDispatcher, OcppMessageDispatcher>();

        // ── OCPP Message Router (scoped - uses IServiceProvider to resolve handlers) ──
        services.AddScoped<OcppMessageRouter>();

        // ── OCPP Message Handlers (scoped - one instance per message) ──
        services.AddScoped<BootNotificationHandler>();
        services.AddScoped<HeartbeatHandler>();
        services.AddScoped<StatusNotificationHandler>();
        services.AddScoped<AuthorizeHandler>();
        services.AddScoped<StartTransactionHandler>();
        services.AddScoped<StopTransactionHandler>();
        services.AddScoped<MeterValuesHandler>();
        services.AddScoped<DataTransferHandler>();
        services.AddScoped<DiagnosticsStatusNotificationHandler>();
        services.AddScoped<FirmwareStatusNotificationHandler>();

        // ── SignalR ──
        services.AddSignalR();

        // ── Background Workers ──
        Configure<AbpBackgroundWorkerOptions>(options =>
        {
            options.IsEnabled = true;
        });
    }

    public override async Task OnApplicationInitializationAsync(
        ApplicationInitializationContext context)
    {
        // Register background workers
        var workerManager = context.ServiceProvider
            .GetRequiredService<IBackgroundWorkerManager>();

        await workerManager.AddAsync(
            context.ServiceProvider
                .GetRequiredService<BackgroundWorkers.HeartbeatMonitorWorker>());

        await base.OnApplicationInitializationAsync(context);
    }
}
