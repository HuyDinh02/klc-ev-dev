using System;
using System.Collections.Generic;
using KLC.Enums;

namespace KLC.Monitoring;

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

// Analytics DTOs
public class GetAnalyticsDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class AnalyticsDto
{
    public List<DailyStatsDto> DailyStats { get; set; } = new();
    public List<StationUtilizationDto> StationUtilization { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public int TotalSessions { get; set; }
    public decimal AverageSessionDurationMinutes { get; set; }
    public decimal UptimePercent { get; set; }

    /// <summary>
    /// Mean time between faults in hours (across all stations in period).
    /// </summary>
    public decimal MtbfHours { get; set; }

    /// <summary>
    /// Peak hour of the day (0-23 UTC) with the most session starts.
    /// </summary>
    public int? PeakHourUtc { get; set; }

    /// <summary>
    /// Number of sessions that started during the peak hour.
    /// </summary>
    public int PeakHourSessionCount { get; set; }
}

public class DailyStatsDto
{
    public string Date { get; set; } = string.Empty;
    public int Sessions { get; set; }
    public decimal EnergyKwh { get; set; }
    public decimal Revenue { get; set; }
}

public class StationUtilizationDto
{
    public Guid StationId { get; set; }
    public string StationName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal UtilizationPercent { get; set; }

    /// <summary>
    /// Percentage of time the station was online (not Offline/Faulted) in the period.
    /// Derived from StatusChangeLog history; falls back to current status snapshot.
    /// </summary>
    public decimal OnlinePercent { get; set; }
}
