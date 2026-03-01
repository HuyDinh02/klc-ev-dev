using System;
using System.Collections.Generic;
using KCharge.Enums;

namespace KCharge.Monitoring;

// Dashboard DTOs
public class DashboardDto
{
    public int TotalStations { get; set; }
    public int OnlineStations { get; set; }
    public int OfflineStations { get; set; }
    public int FaultedStations { get; set; }
    public int TotalConnectors { get; set; }
    public int AvailableConnectors { get; set; }
    public int ChargingConnectors { get; set; }
    public int FaultedConnectors { get; set; }
    public int ActiveSessions { get; set; }
    public decimal TodayEnergyKwh { get; set; }
    public decimal TodayRevenue { get; set; }
    public List<StationStatusSummaryDto> StationSummaries { get; set; } = new();
}

public class StationStatusSummaryDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public StationStatus Status { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int TotalConnectors { get; set; }
    public int AvailableConnectors { get; set; }
    public int ChargingConnectors { get; set; }
    public DateTime? LastHeartbeat { get; set; }
}

// Status Change Log DTOs
public class StatusChangeLogDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public int? ConnectorNumber { get; set; }
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class GetStatusHistoryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? ConnectorNumber { get; set; }
    public Guid? Cursor { get; set; }
    public int MaxResultCount { get; set; } = 50;
}

// Energy Summary DTOs
public class EnergySummaryDto
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public decimal TotalEnergyKwh { get; set; }
    public int TotalSessions { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSessionEnergyKwh { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
}

public class GetEnergySummaryDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
