using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Fleets;
using KLC.Hubs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Services;

/// <summary>
/// Background service that resets fleet daily energy and monthly budget counters.
/// Runs every hour. Checks UTC date boundaries:
///   - Daily: resets vehicle CurrentDayEnergyKwh at midnight UTC
///   - Monthly: resets fleet CurrentMonthSpentVnd and vehicle CurrentMonthEnergyKwh on 1st of month
/// Also checks budget alert thresholds and fires SignalR notifications.
/// </summary>
public class FleetResetBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FleetResetBackgroundService> _logger;

    private DateTime _lastDailyResetDate = DateTime.MinValue;
    private DateTime _lastMonthlyResetDate = DateTime.MinValue;

    public FleetResetBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FleetResetBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FleetResetBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var utcNow = DateTime.UtcNow;

                // Daily reset: run once per day at first check after midnight UTC
                if (utcNow.Date > _lastDailyResetDate)
                {
                    await ResetDailyEnergyAsync(stoppingToken);
                    _lastDailyResetDate = utcNow.Date;
                }

                // Monthly reset: run once per month at first check after 1st of month
                var monthStart = new DateTime(utcNow.Year, utcNow.Month, 1);
                if (monthStart > _lastMonthlyResetDate)
                {
                    await ResetMonthlyBudgetsAsync(stoppingToken);
                    _lastMonthlyResetDate = monthStart;
                }

                // Budget alert check: run every cycle
                await CheckBudgetAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FleetResetBackgroundService cycle");
            }

            // Check every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ResetDailyEnergyAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var vehicleRepo = scope.ServiceProvider.GetRequiredService<IRepository<FleetVehicle, Guid>>();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(requiresNew: true);

        var vehicles = await vehicleRepo.GetListAsync(v => v.IsActive && v.CurrentDayEnergyKwh > 0, cancellationToken: ct);

        foreach (var vehicle in vehicles)
        {
            vehicle.ResetDailyEnergy();
        }

        if (vehicles.Count > 0)
        {
            await vehicleRepo.UpdateManyAsync(vehicles, cancellationToken: ct);
            _logger.LogInformation("Daily energy reset completed for {Count} vehicles", vehicles.Count);
        }

        await uow.CompleteAsync(ct);
    }

    private async Task ResetMonthlyBudgetsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var fleetRepo = scope.ServiceProvider.GetRequiredService<IRepository<Fleet, Guid>>();
        var vehicleRepo = scope.ServiceProvider.GetRequiredService<IRepository<FleetVehicle, Guid>>();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin(requiresNew: true);

        // Reset fleet spending
        var fleets = await fleetRepo.GetListAsync(f => f.IsActive && f.CurrentMonthSpentVnd > 0, cancellationToken: ct);
        foreach (var fleet in fleets)
        {
            fleet.ResetMonthlySpending();
        }

        if (fleets.Count > 0)
        {
            await fleetRepo.UpdateManyAsync(fleets, cancellationToken: ct);
            _logger.LogInformation("Monthly budget reset completed for {Count} fleets", fleets.Count);
        }

        // Reset vehicle monthly energy
        var vehicles = await vehicleRepo.GetListAsync(v => v.IsActive && v.CurrentMonthEnergyKwh > 0, cancellationToken: ct);
        foreach (var vehicle in vehicles)
        {
            vehicle.ResetMonthlyEnergy();
        }

        if (vehicles.Count > 0)
        {
            await vehicleRepo.UpdateManyAsync(vehicles, cancellationToken: ct);
            _logger.LogInformation("Monthly energy reset completed for {Count} vehicles", vehicles.Count);
        }

        await uow.CompleteAsync(ct);
    }

    private async Task CheckBudgetAlertsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var fleetRepo = scope.ServiceProvider.GetRequiredService<IRepository<Fleet, Guid>>();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var notifier = scope.ServiceProvider.GetService<IMonitoringNotifier>();

        if (notifier == null) return;

        using var uow = uowManager.Begin(requiresNew: true);

        var fleets = await fleetRepo.GetListAsync(
            f => f.IsActive && f.MaxMonthlyBudgetVnd > 0,
            cancellationToken: ct);

        foreach (var fleet in fleets)
        {
            var utilization = fleet.GetBudgetUtilizationPercent();
            if (utilization >= fleet.BudgetAlertThresholdPercent)
            {
                var message = utilization >= 100
                    ? $"Fleet \"{fleet.Name}\" has exceeded monthly budget ({utilization:F0}%)"
                    : $"Fleet \"{fleet.Name}\" budget at {utilization:F0}% (threshold: {fleet.BudgetAlertThresholdPercent}%)";

                await notifier.NotifyAlertCreatedAsync(
                    Guid.NewGuid(),
                    null,
                    fleet.Name,
                    "FleetBudgetAlert",
                    message);

                _logger.LogInformation("Fleet budget alert: {FleetName} at {Utilization}%", fleet.Name, utilization);
            }
        }

        await uow.CompleteAsync(ct);
    }
}
