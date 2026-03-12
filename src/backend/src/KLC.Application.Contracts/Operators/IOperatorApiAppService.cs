using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KLC.Enums;
using Volo.Abp.Application.Services;

namespace KLC.Operators;

/// <summary>
/// Application service for the B2B Operator API.
/// Provides read-only access to stations and sessions for an authenticated operator.
/// </summary>
public interface IOperatorApiAppService : IApplicationService
{
    Task<List<OperatorStationListItemDto>> GetStationsAsync(Guid operatorId, string? cursor, int pageSize);
    Task<OperatorStationDetailDto> GetStationAsync(Guid operatorId, Guid stationId);
    Task<List<OperatorSessionDto>> GetSessionsAsync(Guid operatorId, string? cursor, int pageSize, DateTime? fromDate, DateTime? toDate, Guid? stationId);
    Task<List<OperatorSessionDto>> GetActiveSessionsAsync(Guid operatorId);
    Task<OperatorAnalyticsSummaryDto> GetAnalyticsSummaryAsync(Guid operatorId);
}

#region B2B API DTOs

public class OperatorStationListItemDto
{
    public Guid Id { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public StationStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public int ConnectorCount { get; set; }
    public DateTime? LastHeartbeat { get; set; }
}

public class OperatorStationDetailDto
{
    public Guid Id { get; set; }
    public string StationCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public StationStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? Model { get; set; }
    public string? Vendor { get; set; }
    public string? SerialNumber { get; set; }
    public List<OperatorConnectorDto> Connectors { get; set; } = [];
}

public class OperatorConnectorDto
{
    public Guid Id { get; set; }
    public int ConnectorNumber { get; set; }
    public ConnectorType ConnectorType { get; set; }
    public decimal MaxPowerKw { get; set; }
    public ConnectorStatus Status { get; set; }
    public bool IsEnabled { get; set; }
}

public class OperatorSessionDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public int ConnectorNumber { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalCost { get; set; }
    public decimal RatePerKwh { get; set; }
    public string? StopReason { get; set; }
}

public class OperatorAnalyticsSummaryDto
{
    public int TotalStations { get; set; }
    public int OnlineStations { get; set; }
    public int TotalSessionsLast30Days { get; set; }
    public int CompletedSessionsLast30Days { get; set; }
    public int ActiveSessions { get; set; }
    public decimal TotalEnergyKwhLast30Days { get; set; }
    public decimal TotalRevenueLast30Days { get; set; }
    public double AverageSessionDurationMinutes { get; set; }
}

#endregion
