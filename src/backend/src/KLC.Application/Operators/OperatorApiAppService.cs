using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Sessions;
using KLC.Stations;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace KLC.Operators;

/// <summary>
/// Application service for the B2B Operator API.
/// No ABP authorization — authentication is handled by the OperatorApiKeyMiddleware.
/// </summary>
public class OperatorApiAppService : KLCAppService, IOperatorApiAppService
{
    private readonly IRepository<Operator, Guid> _operatorRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;

    public OperatorApiAppService(
        IRepository<Operator, Guid> operatorRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<ChargingSession, Guid> sessionRepository)
    {
        _operatorRepository = operatorRepository;
        _stationRepository = stationRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<List<OperatorStationListItemDto>> GetStationsAsync(
        Guid operatorId, string? cursor, int pageSize)
    {
        var op = await _operatorRepository.GetAsync(operatorId);
        var stationIds = op.AllowedStations
            .Where(s => !s.IsDeleted)
            .Select(s => s.StationId)
            .ToList();

        if (stationIds.Count == 0)
            return [];

        var query = await _stationRepository.GetQueryableAsync();
        query = query.Where(s => stationIds.Contains(s.Id));

        if (!string.IsNullOrWhiteSpace(cursor) && Guid.TryParse(cursor, out var cursorId))
            query = query.Where(s => s.Id.CompareTo(cursorId) > 0);

        query = query.OrderBy(s => s.Id);
        var stations = await AsyncExecuter.ToListAsync(query.Take(pageSize));

        return stations.Select(s => new OperatorStationListItemDto
        {
            Id = s.Id,
            StationCode = s.StationCode,
            Name = s.Name,
            Address = s.Address,
            Latitude = s.Latitude,
            Longitude = s.Longitude,
            Status = s.Status,
            IsEnabled = s.IsEnabled,
            ConnectorCount = s.Connectors.Count,
            LastHeartbeat = s.LastHeartbeat
        }).ToList();
    }

    public async Task<OperatorStationDetailDto> GetStationAsync(Guid operatorId, Guid stationId)
    {
        var op = await _operatorRepository.GetAsync(operatorId);

        if (!op.HasStationAccess(stationId))
            throw new BusinessException(KLCDomainErrorCodes.Operators.NoStationAccess);

        var station = await _stationRepository.GetAsync(stationId);

        return new OperatorStationDetailDto
        {
            Id = station.Id,
            StationCode = station.StationCode,
            Name = station.Name,
            Address = station.Address,
            Latitude = station.Latitude,
            Longitude = station.Longitude,
            Status = station.Status,
            IsEnabled = station.IsEnabled,
            LastHeartbeat = station.LastHeartbeat,
            FirmwareVersion = station.FirmwareVersion,
            Model = station.Model,
            Vendor = station.Vendor,
            SerialNumber = station.SerialNumber,
            Connectors = station.Connectors.Select(c => new OperatorConnectorDto
            {
                Id = c.Id,
                ConnectorNumber = c.ConnectorNumber,
                ConnectorType = c.ConnectorType,
                MaxPowerKw = c.MaxPowerKw,
                Status = c.Status,
                IsEnabled = c.IsEnabled
            }).ToList()
        };
    }

    public async Task<List<OperatorSessionDto>> GetSessionsAsync(
        Guid operatorId, string? cursor, int pageSize,
        DateTime? fromDate, DateTime? toDate, Guid? stationId)
    {
        var op = await _operatorRepository.GetAsync(operatorId);
        var stationIds = op.AllowedStations
            .Where(s => !s.IsDeleted)
            .Select(s => s.StationId)
            .ToList();

        if (stationIds.Count == 0)
            return [];

        if (stationId.HasValue)
        {
            if (!stationIds.Contains(stationId.Value))
                throw new BusinessException(KLCDomainErrorCodes.Operators.NoStationAccess);
            stationIds = [stationId.Value];
        }

        var query = await _sessionRepository.GetQueryableAsync();
        query = query.Where(s => stationIds.Contains(s.StationId));

        if (fromDate.HasValue)
            query = query.Where(s => s.StartTime >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(s => s.StartTime <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(cursor) && Guid.TryParse(cursor, out var cursorId))
            query = query.Where(s => s.Id.CompareTo(cursorId) > 0);

        query = query.OrderByDescending(s => s.StartTime);
        var sessions = await AsyncExecuter.ToListAsync(query.Take(pageSize));

        return sessions.Select(MapSessionDto).ToList();
    }

    public async Task<List<OperatorSessionDto>> GetActiveSessionsAsync(Guid operatorId)
    {
        var op = await _operatorRepository.GetAsync(operatorId);
        var stationIds = op.AllowedStations
            .Where(s => !s.IsDeleted)
            .Select(s => s.StationId)
            .ToList();

        if (stationIds.Count == 0)
            return [];

        var query = await _sessionRepository.GetQueryableAsync();
        query = query.Where(s =>
            stationIds.Contains(s.StationId) &&
            (s.Status == SessionStatus.Starting ||
             s.Status == SessionStatus.InProgress ||
             s.Status == SessionStatus.Suspended ||
             s.Status == SessionStatus.Stopping));

        query = query.OrderByDescending(s => s.StartTime);
        var sessions = await AsyncExecuter.ToListAsync(query);

        return sessions.Select(MapSessionDto).ToList();
    }

    public async Task<OperatorAnalyticsSummaryDto> GetAnalyticsSummaryAsync(Guid operatorId)
    {
        var op = await _operatorRepository.GetAsync(operatorId);
        var stationIds = op.AllowedStations
            .Where(s => !s.IsDeleted)
            .Select(s => s.StationId)
            .ToList();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var sessionQuery = await _sessionRepository.GetQueryableAsync();
        sessionQuery = sessionQuery.Where(s =>
            stationIds.Contains(s.StationId) &&
            s.StartTime >= thirtyDaysAgo);

        var sessions = await AsyncExecuter.ToListAsync(sessionQuery);

        var completedSessions = sessions
            .Where(s => s.Status == SessionStatus.Completed)
            .ToList();

        var activeSessions = sessions
            .Where(s => s.Status == SessionStatus.InProgress ||
                        s.Status == SessionStatus.Starting ||
                        s.Status == SessionStatus.Suspended ||
                        s.Status == SessionStatus.Stopping)
            .ToList();

        var stationQuery = await _stationRepository.GetQueryableAsync();
        stationQuery = stationQuery.Where(s => stationIds.Contains(s.Id));
        var stations = await AsyncExecuter.ToListAsync(stationQuery);

        return new OperatorAnalyticsSummaryDto
        {
            TotalStations = stationIds.Count,
            OnlineStations = stations.Count(s =>
                s.Status != StationStatus.Offline &&
                s.Status != StationStatus.Decommissioned),
            TotalSessionsLast30Days = sessions.Count,
            CompletedSessionsLast30Days = completedSessions.Count,
            ActiveSessions = activeSessions.Count,
            TotalEnergyKwhLast30Days = completedSessions.Sum(s => s.TotalEnergyKwh),
            TotalRevenueLast30Days = completedSessions.Sum(s => s.TotalCost),
            AverageSessionDurationMinutes = completedSessions.Count > 0
                ? completedSessions
                    .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                    .Select(s => (s.EndTime!.Value - s.StartTime!.Value).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Average()
                : 0
        };
    }

    private static OperatorSessionDto MapSessionDto(ChargingSession s)
    {
        return new OperatorSessionDto
        {
            Id = s.Id,
            StationId = s.StationId,
            ConnectorNumber = s.ConnectorNumber,
            Status = s.Status,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            TotalEnergyKwh = s.TotalEnergyKwh,
            TotalCost = s.TotalCost,
            RatePerKwh = s.RatePerKwh,
            StopReason = s.StopReason
        };
    }
}
