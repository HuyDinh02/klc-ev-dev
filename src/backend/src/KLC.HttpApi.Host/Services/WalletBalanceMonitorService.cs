using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Ocpp;
using KLC.Sessions;
using KLC.Stations;
using KLC.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace KLC.Services;

/// <summary>
/// Background service that monitors active charging sessions and auto-stops them
/// when the user's wallet balance minus the session's running cost drops below a threshold.
/// This prevents users from accumulating charges they cannot pay for.
/// </summary>
public class WalletBalanceMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WalletBalanceMonitorService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly decimal _lowBalanceThreshold;

    public WalletBalanceMonitorService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<WalletBalanceMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _lowBalanceThreshold = configuration.GetValue("Wallet:LowBalanceThreshold", 10_000m);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WalletBalanceMonitorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await CheckActiveSessionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WalletBalanceMonitorService");
            }
        }

        _logger.LogInformation("WalletBalanceMonitorService stopped");
    }

    private async Task CheckActiveSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IRepository<ChargingSession, Guid>>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IRepository<AppUser, Guid>>();
        var stationRepository = scope.ServiceProvider.GetRequiredService<IRepository<ChargingStation, Guid>>();
        var remoteCommandService = scope.ServiceProvider.GetRequiredService<IOcppRemoteCommandService>();
        var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = unitOfWorkManager.Begin();

        var activeSessions = await sessionRepository.GetListAsync(
            s => s.Status == SessionStatus.InProgress);

        if (activeSessions.Count == 0)
        {
            return;
        }

        _logger.LogDebug(
            "WalletBalanceMonitor checking {Count} active sessions",
            activeSessions.Count);

        foreach (var session in activeSessions)
        {
            try
            {
                await CheckSessionBalanceAsync(
                    session,
                    userRepository,
                    stationRepository,
                    remoteCommandService);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to check wallet balance for session {SessionId}, user {UserId}",
                    session.Id,
                    session.UserId);
            }
        }

        await uow.CompleteAsync();
    }

    private async Task CheckSessionBalanceAsync(
        ChargingSession session,
        IRepository<AppUser, Guid> userRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IOcppRemoteCommandService remoteCommandService)
    {
        var user = await userRepository.FirstOrDefaultAsync(u => u.IdentityUserId == session.UserId);
        if (user == null)
        {
            _logger.LogWarning(
                "AppUser not found for session {SessionId}, UserId {UserId}",
                session.Id, session.UserId);
            return;
        }

        var remainingBalance = user.WalletBalance - session.TotalCost;

        if (remainingBalance >= _lowBalanceThreshold)
        {
            return;
        }

        _logger.LogWarning(
            "Low wallet balance detected for session {SessionId}. " +
            "UserId: {UserId}, WalletBalance: {WalletBalance}, " +
            "SessionCost: {SessionCost}, Remaining: {Remaining}, Threshold: {Threshold}",
            session.Id,
            session.UserId,
            user.WalletBalance,
            session.TotalCost,
            remainingBalance,
            _lowBalanceThreshold);

        if (!session.OcppTransactionId.HasValue)
        {
            _logger.LogWarning(
                "Session {SessionId} has no OcppTransactionId, cannot send RemoteStopTransaction",
                session.Id);
            return;
        }

        var station = await stationRepository.FirstOrDefaultAsync(s => s.Id == session.StationId);
        if (station == null)
        {
            _logger.LogWarning(
                "Station not found for session {SessionId}, StationId {StationId}",
                session.Id, session.StationId);
            return;
        }

        _logger.LogInformation(
            "Sending RemoteStopTransaction for session {SessionId} " +
            "to station {StationCode}, transactionId {TransactionId} due to low wallet balance",
            session.Id,
            station.StationCode,
            session.OcppTransactionId.Value);

        var result = await remoteCommandService.SendRemoteStopTransactionAsync(
            station.StationCode,
            session.OcppTransactionId.Value);

        if (result.Accepted)
        {
            _logger.LogInformation(
                "RemoteStopTransaction accepted for session {SessionId} on station {StationCode}",
                session.Id, station.StationCode);
        }
        else
        {
            _logger.LogWarning(
                "RemoteStopTransaction rejected for session {SessionId} on station {StationCode}: {ErrorMessage}",
                session.Id, station.StationCode, result.ErrorMessage ?? "Unknown failure");
        }
    }
}
