using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Faults;
using KLC.Permissions;
using KLC.Sessions;
using KLC.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace KLC.Monitoring;

[Authorize(KLCPermissions.Monitoring.Default)]
public class MonitoringAppService : KLCAppService, IMonitoringAppService
{
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<Connector, Guid> _connectorRepository;
    private readonly IRepository<StatusChangeLog, Guid> _statusLogRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<MeterValue, Guid> _meterValueRepository;
    private readonly IRepository<Fault, Guid> _faultRepository;

    public MonitoringAppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<StatusChangeLog, Guid> statusLogRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<MeterValue, Guid> meterValueRepository,
        IRepository<Fault, Guid> faultRepository)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _statusLogRepository = statusLogRepository;
        _sessionRepository = sessionRepository;
        _meterValueRepository = meterValueRepository;
        _faultRepository = faultRepository;
    }

    [Authorize(KLCPermissions.Monitoring.Dashboard)]
    public async Task<DashboardDto> GetDashboardAsync()
    {
        var stations = await _stationRepository.GetListAsync();
        var connectors = await _connectorRepository.GetListAsync();

        var today = DateTime.UtcNow.Date;
        var sessionQuery = await _sessionRepository.GetQueryableAsync();
        var todaySessions = await AsyncExecuter.ToListAsync(
            sessionQuery.Where(s => s.CreationTime >= today));

        var dashboard = new DashboardDto
        {
            TotalStations = stations.Count,
            OnlineStations = stations.Count(s => s.Status == StationStatus.Online),
            OfflineStations = stations.Count(s => s.Status == StationStatus.Offline || s.Status == StationStatus.Disabled),
            FaultedStations = 0, // Faults are tracked at connector level, not station level
            TotalConnectors = connectors.Count,
            AvailableConnectors = connectors.Count(c => c.Status == ConnectorStatus.Available),
            ChargingConnectors = connectors.Count(c => c.Status == ConnectorStatus.Charging),
            FaultedConnectors = connectors.Count(c => c.Status == ConnectorStatus.Faulted),
            ActiveSessions = todaySessions.Count(s => s.Status == SessionStatus.InProgress),
            TodayEnergyKwh = todaySessions.Sum(s => s.TotalEnergyKwh),
            TodayRevenue = todaySessions.Sum(s => s.TotalCost),
            StationSummaries = stations.Select(s => new StationStatusSummaryDto
            {
                StationId = s.Id,
                StationName = s.Name,
                Status = s.Status,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                TotalConnectors = connectors.Count(c => c.StationId == s.Id),
                AvailableConnectors = connectors.Count(c => c.StationId == s.Id && c.Status == ConnectorStatus.Available),
                ChargingConnectors = connectors.Count(c => c.StationId == s.Id && c.Status == ConnectorStatus.Charging),
                LastHeartbeat = s.LastHeartbeat
            }).ToList()
        };

        return dashboard;
    }

    [Authorize(KLCPermissions.Monitoring.StatusHistory)]
    public async Task<PagedResultDto<StatusChangeLogDto>> GetStatusHistoryAsync(Guid stationId, GetStatusHistoryDto input)
    {
        var query = await _statusLogRepository.GetQueryableAsync();
        query = query.Where(l => l.StationId == stationId);

        if (input.ConnectorNumber.HasValue)
        {
            query = query.Where(l => l.ConnectorNumber == input.ConnectorNumber.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= input.ToDate.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(l => l.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(l => l.Timestamp);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var logs = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        var dtos = logs.Select(l => new StatusChangeLogDto
        {
            Id = l.Id,
            StationId = l.StationId,
            ConnectorNumber = l.ConnectorNumber,
            PreviousStatus = l.PreviousStatus,
            NewStatus = l.NewStatus,
            Timestamp = l.Timestamp,
            Source = l.Source,
            Details = l.Details
        }).ToList();

        return new PagedResultDto<StatusChangeLogDto>(totalCount, dtos);
    }

    [Authorize(KLCPermissions.Monitoring.EnergySummary)]
    public async Task<EnergySummaryDto> GetStationEnergySummaryAsync(Guid stationId, GetEnergySummaryDto input)
    {
        var station = await _stationRepository.GetAsync(stationId);
        var sessionQuery = await _sessionRepository.GetQueryableAsync();

        sessionQuery = sessionQuery.Where(s => s.StationId == stationId && s.Status == SessionStatus.Completed);

        if (input.FromDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.CreationTime <= input.ToDate.Value);
        }

        var sessions = await AsyncExecuter.ToListAsync(sessionQuery);

        var totalEnergy = sessions.Sum(s => s.TotalEnergyKwh);
        var totalRevenue = sessions.Sum(s => s.TotalCost);
        var totalDuration = sessions
            .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
            .Sum(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes);

        return new EnergySummaryDto
        {
            EntityId = stationId,
            EntityName = station.Name,
            TotalEnergyKwh = totalEnergy,
            TotalSessions = sessions.Count,
            TotalRevenue = totalRevenue,
            AverageSessionEnergyKwh = sessions.Count > 0 ? totalEnergy / sessions.Count : 0,
            AverageSessionDurationMinutes = sessions.Count > 0 ? (decimal)(totalDuration / sessions.Count) : 0
        };
    }

    [Authorize(KLCPermissions.Monitoring.EnergySummary)]
    public async Task<EnergySummaryDto> GetConnectorEnergySummaryAsync(Guid connectorId, GetEnergySummaryDto input)
    {
        var connector = await _connectorRepository.GetAsync(connectorId);
        var station = await _stationRepository.GetAsync(connector.StationId);
        var sessionQuery = await _sessionRepository.GetQueryableAsync();

        sessionQuery = sessionQuery.Where(s =>
            s.StationId == connector.StationId &&
            s.ConnectorNumber == connector.ConnectorNumber &&
            s.Status == SessionStatus.Completed);

        if (input.FromDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            sessionQuery = sessionQuery.Where(s => s.CreationTime <= input.ToDate.Value);
        }

        var sessions = await AsyncExecuter.ToListAsync(sessionQuery);

        var totalEnergy = sessions.Sum(s => s.TotalEnergyKwh);
        var totalRevenue = sessions.Sum(s => s.TotalCost);
        var totalDuration = sessions
            .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
            .Sum(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes);

        return new EnergySummaryDto
        {
            EntityId = connectorId,
            EntityName = $"{station.Name} - Connector {connector.ConnectorNumber}",
            TotalEnergyKwh = totalEnergy,
            TotalSessions = sessions.Count,
            TotalRevenue = totalRevenue,
            AverageSessionEnergyKwh = sessions.Count > 0 ? totalEnergy / sessions.Count : 0,
            AverageSessionDurationMinutes = sessions.Count > 0 ? (decimal)(totalDuration / sessions.Count) : 0
        };
    }

    [Authorize(KLCPermissions.Monitoring.Dashboard)]
    public async Task<AnalyticsDto> GetAnalyticsAsync(GetAnalyticsDto input)
    {
        var fromDate = input.FromDate ?? DateTime.UtcNow.AddDays(-30);
        var toDate = input.ToDate ?? DateTime.UtcNow;

        var sessionQuery = await _sessionRepository.GetQueryableAsync();
        var sessions = await AsyncExecuter.ToListAsync(
            sessionQuery.Where(s => s.Status == SessionStatus.Completed
                && s.CreationTime >= fromDate
                && s.CreationTime <= toDate));

        var stations = await _stationRepository.GetListAsync();
        var connectors = await _connectorRepository.GetListAsync();

        // Status change logs for the period (for per-station online %)
        var statusLogQuery = await _statusLogRepository.GetQueryableAsync();
        var statusLogs = await AsyncExecuter.ToListAsync(
            statusLogQuery.Where(l => l.ConnectorNumber == null && l.Timestamp >= fromDate && l.Timestamp <= toDate)
                .OrderBy(l => l.Timestamp));

        // Faults for MTBF calculation
        var faultQuery = await _faultRepository.GetQueryableAsync();
        var faults = await AsyncExecuter.ToListAsync(
            faultQuery.Where(f => f.DetectedAt >= fromDate && f.DetectedAt <= toDate)
                .OrderBy(f => f.DetectedAt));

        // Daily stats
        var dailyStats = sessions
            .GroupBy(s => s.CreationTime.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyStatsDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Sessions = g.Count(),
                EnergyKwh = Math.Round(g.Sum(s => s.TotalEnergyKwh), 2),
                Revenue = Math.Round(g.Sum(s => s.TotalCost), 0)
            })
            .ToList();

        // Station utilization: sessions per station / total hours in period
        var totalHours = (toDate - fromDate).TotalHours;
        var statusLogsByStation = statusLogs.GroupBy(l => l.StationId).ToDictionary(g => g.Key, g => g.ToList());

        var stationUtilization = stations.Select(station =>
        {
            var stationSessions = sessions.Where(s => s.StationId == station.Id).ToList();
            var stationConnectorCount = connectors.Count(c => c.StationId == station.Id);
            var chargingHours = stationSessions
                .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                .Sum(s => (s.EndTime!.Value - s.StartTime!.Value).TotalHours);
            var maxCapacityHours = stationConnectorCount * totalHours;
            var utilization = maxCapacityHours > 0 ? (decimal)(chargingHours / maxCapacityHours * 100) : 0;

            // Per-station online % from status change logs
            var onlinePercent = CalculateStationOnlinePercent(
                station, statusLogsByStation.GetValueOrDefault(station.Id), fromDate, toDate);

            return new StationUtilizationDto
            {
                StationId = station.Id,
                StationName = station.Name,
                TotalSessions = stationSessions.Count,
                TotalEnergyKwh = Math.Round(stationSessions.Sum(s => s.TotalEnergyKwh), 2),
                TotalRevenue = Math.Round(stationSessions.Sum(s => s.TotalCost), 0),
                UtilizationPercent = Math.Round(utilization, 1),
                OnlinePercent = onlinePercent
            };
        })
        .OrderByDescending(s => s.TotalSessions)
        .ToList();

        // Uptime: % of stations that are not Faulted/Offline
        var onlineStations = stations.Count(s => s.Status == StationStatus.Online);
        var activeStations = stations.Count(s => s.Status != StationStatus.Decommissioned);
        var uptimePercent = activeStations > 0 ? Math.Round((decimal)onlineStations / activeStations * 100, 1) : 0;

        var totalDuration = sessions
            .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
            .Sum(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes);

        // MTBF: total station-hours in period / number of faults
        var totalStationHours = (decimal)(activeStations * totalHours);
        var faultCount = faults.Count;
        var mtbfHours = faultCount > 0 ? Math.Round(totalStationHours / faultCount, 1) : 0;

        // Peak hour: hour of day (UTC) with the most session starts
        int? peakHourUtc = null;
        var peakHourSessionCount = 0;
        var sessionsWithStart = sessions.Where(s => s.StartTime.HasValue).ToList();
        if (sessionsWithStart.Count > 0)
        {
            var hourGroups = sessionsWithStart
                .GroupBy(s => s.StartTime!.Value.Hour)
                .OrderByDescending(g => g.Count())
                .First();
            peakHourUtc = hourGroups.Key;
            peakHourSessionCount = hourGroups.Count();
        }

        return new AnalyticsDto
        {
            DailyStats = dailyStats,
            StationUtilization = stationUtilization,
            TotalRevenue = Math.Round(sessions.Sum(s => s.TotalCost), 0),
            TotalEnergyKwh = Math.Round(sessions.Sum(s => s.TotalEnergyKwh), 2),
            TotalSessions = sessions.Count,
            AverageSessionDurationMinutes = sessions.Count > 0 ? Math.Round((decimal)(totalDuration / sessions.Count), 1) : 0,
            UptimePercent = uptimePercent,
            MtbfHours = mtbfHours,
            PeakHourUtc = peakHourUtc,
            PeakHourSessionCount = peakHourSessionCount
        };
    }

    /// <summary>
    /// Calculates the percentage of time a station was online (not Offline/Faulted)
    /// within the given period, based on status change logs.
    /// Falls back to current status snapshot when no logs exist.
    /// </summary>
    private static decimal CalculateStationOnlinePercent(
        ChargingStation station,
        List<StatusChangeLog>? logs,
        DateTime fromDate,
        DateTime toDate)
    {
        var totalSeconds = (toDate - fromDate).TotalSeconds;
        if (totalSeconds <= 0) return 0;

        if (logs == null || logs.Count == 0)
        {
            // No logs in period — use current status as best estimate
            var isOnline = station.Status == StationStatus.Online;
            return isOnline ? 100m : 0m;
        }

        // Walk through status transitions to accumulate online time
        var onlineStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(StationStatus.Online)
        };

        double onlineSeconds = 0;

        // Determine initial status at fromDate from the first log's PreviousStatus
        var firstLog = logs[0];
        var wasOnline = onlineStatuses.Contains(firstLog.PreviousStatus);
        var lastTimestamp = fromDate;

        foreach (var log in logs)
        {
            if (wasOnline)
            {
                onlineSeconds += (log.Timestamp - lastTimestamp).TotalSeconds;
            }
            wasOnline = onlineStatuses.Contains(log.NewStatus);
            lastTimestamp = log.Timestamp;
        }

        // Account for time from last log to end of period
        if (wasOnline)
        {
            onlineSeconds += (toDate - lastTimestamp).TotalSeconds;
        }

        return Math.Round((decimal)(onlineSeconds / totalSeconds * 100), 1);
    }
}
