using System;
using KLC.Enums;

namespace KLC.Sessions;

/// <summary>
/// Input for starting a charging session via the BFF.
/// Supports flexible connector resolution: by connectorId, stationId+connectorNumber,
/// or stationCode+connectorNumber.
/// </summary>
public class StartSessionInput
{
    public Guid UserId { get; set; }
    public Guid? StationId { get; set; }
    public string? StationCode { get; set; }
    public Guid? ConnectorId { get; set; }
    public int? ConnectorNumber { get; set; }
    public Guid? VehicleId { get; set; }
}

/// <summary>
/// Result of starting a charging session.
/// </summary>
public class StartSessionResultDto
{
    public bool Success { get; set; }
    public Guid? SessionId { get; set; }
    public SessionStatus? Status { get; set; }
    public Guid? StationId { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Result of stopping a charging session.
/// </summary>
public class StopSessionResultDto
{
    public bool Success { get; set; }
    public Guid? SessionId { get; set; }
    public SessionStatus? Status { get; set; }
    public Guid? StationId { get; set; }
    public string? Error { get; set; }
}
