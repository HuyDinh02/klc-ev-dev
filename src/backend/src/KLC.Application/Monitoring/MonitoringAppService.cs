using System;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
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

    public MonitoringAppService(
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<Connector, Guid> connectorRepository,
        IRepository<StatusChangeLog, Guid> statusLogRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<MeterValue, Guid> meterValueRepository)
    {
        _stationRepository = stationRepository;
        _connectorRepository = connectorRepository;
        _statusLogRepository = statusLogRepository;
        _sessionRepository = sessionRepository;
        _meterValueRepository = meterValueRepository;
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
            OnlineStations = stations.Count(s => s.Status == StationStatus.Available || s.Status == StationStatus.Occupied),
            OfflineStations = stations.Count(s => s.Status == StationStatus.Offline),
            FaultedStations = stations.Count(s => s.Status == StationStatus.Faulted),
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
}
